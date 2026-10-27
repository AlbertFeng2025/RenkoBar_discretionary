#region Using declarations
using System;
using System.IO;
using System.Text;
using System.Globalization;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Windows.Media;                       // Brushes
using NinjaTrader.Cbi;
using NinjaTrader.Data;
using NinjaTrader.Gui.Tools;                       // SimpleFont
using NinjaTrader.NinjaScript;
using NinjaTrader.NinjaScript.DrawingTools;        // Draw.TextFixed / TextPosition
#endregion

// Book choice for each setup slot. GLOBAL namespace on purpose (NinjaTrader's generated code in other
// namespaces must see it) and a unique name so it never collides with the other indicators' enums.
public enum RecorderBookType { MERGED, LONG, SHORT }
public enum RecorderHudCorner { BottomLeft, BottomRight, TopLeft, TopRight, Center }

// =============================================================================
// Indicator_Renko_MultiRecorder  —  up to 10 Renko transit setups in ONE indicator (observer)
// =============================================================================
// v1.1 2026-10-27
//   * HUD first line reminds that it is the short version (full strings: Renko_Recorder_Viewer.html).
//   * "0s in a row" on the deepest (last-layer) string: number of 0's after its last 1
//     (all 0's -> the whole length). Shown on the HUD line, in the state file ("trail0",
//     per line) and as the last CSV column "trail0" (deepest string).
//   * Defaults: 9 setups, MERGED / LONG / SHORT x F1 = 10, 100, 1000, all with F2 = 01,10
//     (Layer 2), F3 blank, alert 1000. Setup 10 blank.
// v1  2026-10-27
//
// PURPOSE
//   Does NOT trade. Runs up to 10 setups (book + F1/F2/F3 + alert) on the same Renko chart,
//   using exactly the same string rules as Indicator_Renko_mergedLayer3 / _LONGLayer3 /
//   _SHORTLayer3, and
//     * shows ONE compact line per setup on its panel (optional: "Show HUD"),
//     * writes one CSV log per setup (every new F1 outcome, full strings),
//     * writes one small STATE file <LogBaseName>_state.json after every brick, which the
//       browser viewer (Renko_Recorder_Viewer.html) reads to show the ENTIRE strings,
//     * gives the last-layer alert per setup (one sound per match, alert hours in ET).
//
// SETUP SLOTS ("Setup 01" .. "Setup 10")
//   Book   MERGED / LONG / SHORT (drop-down)
//   F1     blank = slot OFF.  F2 blank = Layer 1.  F3 blank = Layer 2.  F3 needs F2.
//   Alert  last-layer alert pattern, default 1000 (blank = no alert).
//   Patterns: '0' '1', '?' = one-or-more 1s, '*' = one-or-more 0s, comma = OR, tail match.
//   A pattern with any other character turns that slot OFF with a clear message in the
//   NinjaScript Output window, in the log folder (<LogBaseName>_setup_check.txt) and on the HUD.
//
// HOW THE STRINGS ARE BUILT (same as the single indicators)
//   Brick bit: green (close rose) = 1, red (close fell) = 0; equal close repeats the last bit.
//   LONG  book: raw = bricks as is;  F1 match -> next brick GREEN = '1', RED = '0'.
//   SHORT book: raw = bricks flipped (red=1); F1 match -> next brick RED = '1'.
//   MERGED    : F1 tested on BOTH raw strings, outcomes interleaved in ONE F1 string.
//   F2 on the F1 string, F3 on the F2 string: on a tail match the NEXT outcome is copied.
//   The deepest active string is the would-be money string; "MONEY NEXT" = F2 (+F3) armed.
//
// HISTORY / SESSIONS
//   * Include back fill (default ON): on the first live brick the stored bricks are replayed
//     (max "Back-fill max bricks", default 1000), then the strings keep growing live.
//       - "Back fill may cross the session start" (default OFF): OFF = replay stops at the
//         current session's first brick; ON = replay goes further back (up to the max).
//   * Reset strings on new session (default ON): OFF = the strings continue across sessions.
//   * Brick gap (missed bricks): back fill ON -> rebuilt from history; OFF -> clean restart.
//   * Replayed bricks never sound an alert; only new live bricks can.
//   * Without back fill the previous closed brick is used as a 1-brick seed (raw only).
//
// ALERT (per setup)
//   Each new bit on the deepest string whose tail matches the setup's alert pattern -> ONE
//   sound + one row in NinjaTrader's Alerts window + "<<< ALARM" on that setup's HUD line.
//   The line then keeps "last alert hh:mm:ss". Alert hours are New York time (ET), default
//   9:30-16:00 ET (= 6:30 AM - 1:00 PM PT). Outside the hours: no sound, text still updates.
//
// FILES (in Log Folder)
//   <LogBaseName>_S<n>_<BOOK>_F1-<p>_F2-<p>_F3-<p>_<stamp>.csv   one per setup per enable
//      timestamp,brick_time,replay,side,bit,inF2,inF3,f1_pct,f1_roll,f2_pct,f2_roll,f3_pct,f3_roll,f1,f2,f3,alarm,trail0
//   <LogBaseName>_state.json     current state of all setups (rewritten every brick)
//   <LogBaseName>_setup_check.txt   what each slot was understood as (written on enable)
//   ('*' is written as 'z', '?' as 'o', ',' as '_' in file names)
// =============================================================================
namespace NinjaTrader.NinjaScript.Indicators
{
    public class Indicator_Renko_MultiRecorder : Indicator
    {
        // ── one setup (slot) ─────────────────────────────────────────────────
        private class Setup
        {
            public int N; public RecorderBookType Book;
            public string P1, P2, P3, PA;                       // as typed
            public readonly List<string> p1 = new List<string>(), p2 = new List<string>(),
                                         p3 = new List<string>(), pa = new List<string>();
            public bool On; public string Problem = "";
            public readonly StringBuilder RawL = new StringBuilder(), RawS = new StringBuilder();
            public readonly StringBuilder F1 = new StringBuilder(), F2 = new StringBuilder(), F3 = new StringBuilder();
            public bool WaitL, WaitS, A2, A3;
            public bool AlarmNow; public DateTime LastAlert = DateTime.MinValue; public int AlertCount;
            public string LogPath;
            public int Layer { get { return p3.Count > 0 ? 3 : p2.Count > 0 ? 2 : 1; } }
            public StringBuilder Deepest { get { return p3.Count > 0 ? F3 : p2.Count > 0 ? F2 : F1; } }
            public string Label
            {
                get
                {
                    return "S" + N.ToString("00") + " " + Book + " F1=" + P1
                         + (p2.Count > 0 ? " F2=" + P2 : "") + (p3.Count > 0 ? " F3=" + P3 : "");
                }
            }
            public void Reset()
            {
                RawL.Clear(); RawS.Clear(); F1.Clear(); F2.Clear(); F3.Clear();
                WaitL = WaitS = A2 = A3 = false; AlarmNow = false;
            }
        }

        private struct Outcome { public char Side, Bit; public bool InF2, InF3; }

        private readonly List<Setup> setups = new List<Setup>();
        private int  prevBit = -1;                  // last brick bit (green=1), for equal-close carry
        private bool lifeStarted = false, firstBrickDone = false, needReplay = false;
        private int  lastProcessedBar = -1;
        private DateTime since = DateTime.MinValue; // time of the first brick in the strings
        private string historyNote = "";
        private TimeZoneInfo nyZone = null;
        private const int MAX_LEN = 20000;          // keep at most this many bits per string (tail)
        private const string HUD_TAG = "Renko_MultiRecorder_HUD";

        protected override void OnStateChange()
        {
            if (State == State.SetDefaults)
            {
                Name        = "Indicator_Renko_MultiRecorder";
                Description = "Observer: up to 10 Renko transit setups (MERGED/LONG/SHORT, F1/F2/F3) in one indicator, "
                            + "compact HUD, per-setup logs, state file for the browser viewer, last-layer alerts. Does not trade.";
                Calculate        = Calculate.OnBarClose;
                IsOverlay        = false;
                DrawOnPricePanel = false;
                IsAutoScale      = false;
                DisplayInDataBox = false;
                PaintPriceMarkers = false;
                IsSuspendedWhileInactive = false;
                MaximumBarsLookBack = MaximumBarsLookBack.Infinite;

                Setup1Book = RecorderBookType.MERGED; Setup1F1 = "10"; Setup1F2 = "01,10"; Setup1F3 = ""; Setup1Alert = "1000";
                Setup2Book = RecorderBookType.MERGED; Setup2F1 = "100"; Setup2F2 = "01,10"; Setup2F3 = ""; Setup2Alert = "1000";
                Setup3Book = RecorderBookType.MERGED; Setup3F1 = "1000"; Setup3F2 = "01,10"; Setup3F3 = ""; Setup3Alert = "1000";
                Setup4Book = RecorderBookType.LONG; Setup4F1 = "10"; Setup4F2 = "01,10"; Setup4F3 = ""; Setup4Alert = "1000";
                Setup5Book = RecorderBookType.LONG; Setup5F1 = "100"; Setup5F2 = "01,10"; Setup5F3 = ""; Setup5Alert = "1000";
                Setup6Book = RecorderBookType.LONG; Setup6F1 = "1000"; Setup6F2 = "01,10"; Setup6F3 = ""; Setup6Alert = "1000";
                Setup7Book = RecorderBookType.SHORT; Setup7F1 = "10"; Setup7F2 = "01,10"; Setup7F3 = ""; Setup7Alert = "1000";
                Setup8Book = RecorderBookType.SHORT; Setup8F1 = "100"; Setup8F2 = "01,10"; Setup8F3 = ""; Setup8Alert = "1000";
                Setup9Book = RecorderBookType.SHORT; Setup9F1 = "1000"; Setup9F2 = "01,10"; Setup9F3 = ""; Setup9Alert = "1000";
                Setup10Book = RecorderBookType.MERGED; Setup10F1 = ""; Setup10F2 = ""; Setup10F3 = ""; Setup10Alert = "1000";

                IncludeBackFill        = true;
                BackFillMaxBricks      = 1000;
                BackFillCrossSession   = false;
                ResetOnNewSession      = true;
                ShowHud                = true;
                HudTailBits            = 30;
                RollingWindowBits      = 20;
                BitFontSize            = 12;
                HudCorner              = RecorderHudCorner.TopLeft;
                EnableLog              = true;
                LogFolder              = @"C:\temp";
                LogBaseName            = "Renko_MultiRecorder";
                AlertSoundFile         = "Alert2.wav";
                EnableAlertHours       = true;
                AlertStartHourET = 9;  AlertStartMinuteET = 30;
                AlertEndHourET   = 16; AlertEndMinuteET   = 0;
            }
            else if (State == State.Realtime)
            {
                if (!lifeStarted)
                {
                    lifeStarted = true;
                    BuildSetups();
                    needReplay = true;      // first live brick: back fill (or seed) then live
                }
            }
        }

        // =====================================================================
        // slots -> setups (with checks)
        // =====================================================================
        private void BuildSetups()
        {
            setups.Clear();
            AddSlot(1, Setup1Book, Setup1F1, Setup1F2, Setup1F3, Setup1Alert);
            AddSlot(2, Setup2Book, Setup2F1, Setup2F2, Setup2F3, Setup2Alert);
            AddSlot(3, Setup3Book, Setup3F1, Setup3F2, Setup3F3, Setup3Alert);
            AddSlot(4, Setup4Book, Setup4F1, Setup4F2, Setup4F3, Setup4Alert);
            AddSlot(5, Setup5Book, Setup5F1, Setup5F2, Setup5F3, Setup5Alert);
            AddSlot(6, Setup6Book, Setup6F1, Setup6F2, Setup6F3, Setup6Alert);
            AddSlot(7, Setup7Book, Setup7F1, Setup7F2, Setup7F3, Setup7Alert);
            AddSlot(8, Setup8Book, Setup8F1, Setup8F2, Setup8F3, Setup8Alert);
            AddSlot(9, Setup9Book, Setup9F1, Setup9F2, Setup9F3, Setup9Alert);
            AddSlot(10, Setup10Book, Setup10F1, Setup10F2, Setup10F3, Setup10Alert);
            var sb = new StringBuilder();
            sb.AppendLine("Indicator_Renko_MultiRecorder setup check  " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
            foreach (Setup s in setups)
            {
                string line = s.On
                    ? "Setup " + s.N.ToString("00") + "  ON   " + s.Book + "  F1=[" + string.Join(",", s.p1) + "]  F2=["
                      + string.Join(",", s.p2) + "]  F3=[" + string.Join(",", s.p3) + "]  alert=[" + string.Join(",", s.pa)
                      + "]  -> Layer " + s.Layer + (s.Problem.Length > 0 ? "   NOTE: " + s.Problem : "")
                    : "Setup " + s.N.ToString("00") + "  OFF  " + (s.Problem.Length > 0 ? s.Problem : "(F1 blank)");
                sb.AppendLine(line);
                Print("[MultiRecorder] " + line);
            }
            if (EnableLog)
                try
                {
                    if (!Directory.Exists(LogFolder)) Directory.CreateDirectory(LogFolder);
                    File.WriteAllText(Path.Combine(LogFolder, LogBaseName + "_setup_check.txt"), sb.ToString());
                }
                catch { }
        }

        private void AddSlot(int n, RecorderBookType book, string f1, string f2, string f3, string alert)
        {
            var s = new Setup { N = n, Book = book, P1 = (f1 ?? "").Trim(), P2 = (f2 ?? "").Trim(), P3 = (f3 ?? "").Trim(), PA = (alert ?? "").Trim() };
            string bad;
            if (s.P1.Length == 0) { s.On = false; setups.Add(s); return; }
            if (!ParseChecked(s.P1, s.p1, out bad)) { s.On = false; s.Problem = "F1 '" + s.P1 + "' " + bad + " -> setup OFF"; setups.Add(s); return; }
            if (!ParseChecked(s.P2, s.p2, out bad)) { s.On = false; s.Problem = "F2 '" + s.P2 + "' " + bad + " -> setup OFF"; setups.Add(s); return; }
            if (!ParseChecked(s.P3, s.p3, out bad)) { s.On = false; s.Problem = "F3 '" + s.P3 + "' " + bad + " -> setup OFF"; setups.Add(s); return; }
            if (!ParseChecked(s.PA, s.pa, out bad)) { s.On = false; s.Problem = "Alert '" + s.PA + "' " + bad + " -> setup OFF"; setups.Add(s); return; }
            if (s.p2.Count == 0 && s.p3.Count > 0) { s.p3.Clear(); s.Problem = "F2 is blank -> F3 ignored (F3 needs F2)"; }
            s.On = true;
            setups.Add(s);
        }

        // strict parser: comma OR; each token only 0 1 * ?; returns false with a reason on any bad character
        private static bool ParseChecked(string src, List<string> dst, out string bad)
        {
            dst.Clear(); bad = "";
            if (string.IsNullOrWhiteSpace(src)) return true;
            foreach (string tok in src.Split(','))
            {
                string t = (tok ?? "").Trim();
                if (t.Length == 0) continue;
                foreach (char c in t)
                    if (c != '0' && c != '1' && c != '*' && c != '?')
                    { bad = "has an invalid character '" + c + "' (only 0 1 * ? and commas)"; dst.Clear(); return false; }
                dst.Add(t);
            }
            return true;
        }

        // =====================================================================
        // OnBarUpdate
        // =====================================================================
        protected override void OnBarUpdate()
        {
            if (State != State.Realtime) return;
            if (CurrentBar < 2) return;

            if (firstBrickDone && Bars.IsFirstBarOfSession && ResetOnNewSession)
            {
                ResetAll("new trading session");
                since = Time[0]; historyNote = "since new session " + Time[0].ToString("HH:mm:ss");
            }
            else if (lastProcessedBar >= 0 && CurrentBar > lastProcessedBar + 1)
            {
                string why = "brick gap: " + lastProcessedBar + " -> " + CurrentBar;
                if (IncludeBackFill) { needReplay = true; NoteAll("# " + why + " -> rebuilt from history"); }
                else { ResetAll(why); since = Time[0]; historyNote = "since gap restart " + Time[0].ToString("HH:mm:ss"); }
            }
            lastProcessedBar = CurrentBar;
            firstBrickDone = true;

            if (needReplay)
            {
                needReplay = false;
                if (IncludeBackFill) Replay();
                else
                {
                    ResetAll("indicator enabled (no back fill)");
                    since = Time[0]; historyNote = "LIVE since " + Time[0].ToString("HH:mm:ss");
                    // 1-brick seed: previous closed brick into the raw strings only
                    int sb = BitFrom(Close[1], Close[2]);
                    foreach (Setup s in setups) if (s.On) SeedRaw(s, sb);
                    prevBit = sb;
                }
            }

            // ── this live brick ──
            int bit = BitFrom(Close[0], Close[1]);
            foreach (Setup s in setups)
            {
                if (!s.On) continue;
                int len0 = s.Deepest.Length;
                var outs = Process(s, bit);
                foreach (Outcome o in outs) WriteRow(s, o, false, Time[0]);
                CheckAlert(s, len0);
            }
            prevBit = bit;

            WriteState();
            if (ShowHud) UpdateHud();
        }

        private int BitFrom(double close, double prevClose)
        {
            if (close > prevClose) return 1;
            if (close < prevClose) return 0;
            return prevBit >= 0 ? prevBit : 0;
        }

        // Replay stored bricks (Close[k..1]) through every setup; no alerts.
        private void Replay()
        {
            ResetAll("back fill");
            int maxN = Math.Max(1, BackFillMaxBricks), k = 0;
            string stop = "Back-fill max bricks (" + maxN + ") reached";
            if (!BackFillCrossSession && Bars.IsFirstBarOfSession) stop = "this brick opens a new session";
            else
                for (int i = 1; i <= maxN; i++)
                {
                    if (i + 2 > CurrentBar) { stop = "no older bricks loaded (increase Days to load)"; break; }
                    k = i;
                    if (!BackFillCrossSession && Bars.IsFirstBarOfSessionByIndex(CurrentBar - i)) { stop = "session start reached"; break; }
                }
            prevBit = -1;
            for (int i = k; i >= 1; i--)
            {
                int b = BitFrom(Close[i], Close[i + 1]);
                foreach (Setup s in setups)
                {
                    if (!s.On) continue;
                    var outs = Process(s, b);
                    foreach (Outcome o in outs) WriteRow(s, o, true, Time[i]);
                }
                prevBit = b;
            }
            since = k > 0 ? Time[k] : Time[0];
            historyNote = "back fill " + k + " bricks from " + since.ToString("MM-dd HH:mm:ss")
                        + (BackFillCrossSession ? " (may cross sessions)" : "") + "; stopped: " + stop;
            NoteAll("# BACKFILL: " + historyNote);
            Print("[MultiRecorder] " + historyNote);
        }

        // ── the string rules (identical to the single indicators) ──────────────
        private static void SeedRaw(Setup s, int bit)
        {
            s.RawL.Append(bit == 1 ? '1' : '0');
            s.RawS.Append(bit == 1 ? '0' : '1');
        }

        private static List<Outcome> Process(Setup s, int bit)
        {
            var outs = new List<Outcome>(2);
            if (s.WaitL) { s.WaitL = false; outs.Add(AddOutcome(s, 'L', bit == 1 ? '1' : '0')); }
            if (s.WaitS) { s.WaitS = false; outs.Add(AddOutcome(s, 'S', bit == 0 ? '1' : '0')); }
            s.RawL.Append(bit == 1 ? '1' : '0');
            s.RawS.Append(bit == 1 ? '0' : '1');
            if (s.RawL.Length > 4096) s.RawL.Remove(0, s.RawL.Length - 4096);
            if (s.RawS.Length > 4096) s.RawS.Remove(0, s.RawS.Length - 4096);
            if (s.Book != RecorderBookType.SHORT && MatchAny(s.RawL.ToString(), s.p1)) s.WaitL = true;
            if (s.Book != RecorderBookType.LONG  && MatchAny(s.RawS.ToString(), s.p1)) s.WaitS = true;
            return outs;
        }

        private static Outcome AddOutcome(Setup s, char side, char b)
        {
            var o = new Outcome { Side = side, Bit = b };
            if (s.p2.Count > 0 && s.A2)
            {
                o.InF2 = true;
                if (s.p3.Count > 0 && s.A3) { o.InF3 = true; Append(s.F3, b); }
                Append(s.F2, b);
                s.A3 = s.p3.Count > 0 && MatchAny(s.F2.ToString(), s.p3);
            }
            Append(s.F1, b);
            s.A2 = s.p2.Count > 0 && MatchAny(s.F1.ToString(), s.p2);
            return o;
        }

        private static void Append(StringBuilder sb, char b)
        { sb.Append(b); if (sb.Length > MAX_LEN) sb.Remove(0, sb.Length - MAX_LEN); }

        private static bool MoneyNext(Setup s)
        { return s.p2.Count > 0 && s.A2 && (s.p3.Count == 0 || s.A3); }

        // ── reset + log files ────────────────────────────────────────────────
        private void ResetAll(string reason)
        {
            prevBit = -1;
            foreach (Setup s in setups)
            {
                if (!s.On) continue;
                s.Reset();
                if (s.LogPath == null) OpenLog(s, reason);
                else Note(s, "# RESET: " + reason + "  " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
            }
        }

        private static string Safe(string p)
        { return string.IsNullOrWhiteSpace(p) ? "off" : p.Replace("*", "z").Replace("?", "o").Replace(",", "_"); }

        private void OpenLog(Setup s, string reason)
        {
            s.LogPath = null;
            if (!EnableLog) return;
            try
            {
                if (!Directory.Exists(LogFolder)) Directory.CreateDirectory(LogFolder);
                s.LogPath = Path.Combine(LogFolder, LogBaseName + "_S" + s.N.ToString("00") + "_" + s.Book
                    + "_F1-" + Safe(s.P1) + "_F2-" + Safe(s.p2.Count > 0 ? s.P2 : "") + "_F3-" + Safe(s.p3.Count > 0 ? s.P3 : "")
                    + "_" + DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss") + ".csv");
                File.AppendAllText(s.LogPath,
                    "# " + s.Label + "  alert=" + s.PA + "  reason=" + reason
                    + "  backfill=" + (IncludeBackFill ? "ON(max " + BackFillMaxBricks + (BackFillCrossSession ? ", cross sessions" : ", this session") + ")" : "OFF")
                    + "  resetOnNewSession=" + ResetOnNewSession + "  rollingWindow=" + RollingWindowBits + Environment.NewLine
                    + "timestamp,brick_time,replay,side,bit,inF2,inF3,f1_pct,f1_roll,f2_pct,f2_roll,f3_pct,f3_roll,f1,f2,f3,alarm,trail0" + Environment.NewLine);
            }
            catch { s.LogPath = null; }
        }

        private void Note(Setup s, string line)
        {
            if (s.LogPath == null) return;
            try { File.AppendAllText(s.LogPath, line + Environment.NewLine); } catch { }
        }
        private void NoteAll(string line) { foreach (Setup s in setups) if (s.On) Note(s, line); }

        private void WriteRow(Setup s, Outcome o, bool replay, DateTime brickTime)
        {
            if (s.LogPath == null) return;
            var ci = CultureInfo.InvariantCulture;
            try
            {
                string row = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff", ci) + "," + brickTime.ToString("yyyy-MM-dd HH:mm:ss", ci)
                    + "," + (replay ? "1" : "0") + "," + o.Side + "," + o.Bit + "," + (o.InF2 ? "1" : "0") + "," + (o.InF3 ? "1" : "0")
                    + "," + PctS(s.F1) + "," + RollS(s.F1) + "," + PctS(s.F2) + "," + RollS(s.F2) + "," + PctS(s.F3) + "," + RollS(s.F3)
                    + "," + s.F1 + "," + s.F2 + "," + s.F3 + ",," + Trail0(s.Deepest);
                File.AppendAllText(s.LogPath, row + Environment.NewLine);
            }
            catch { }
        }

        // ── percentages ──────────────────────────────────────────────────────
        private static int Ones(StringBuilder s, int from)
        { int n = 0; for (int i = from; i < s.Length; i++) if (s[i] == '1') n++; return n; }
        private static double Pct(StringBuilder s) { return s.Length > 0 ? 100.0 * Ones(s, 0) / s.Length : -1; }
        private double Roll(StringBuilder s)
        {
            if (RollingWindowBits <= 0 || s.Length == 0) return -1;
            int w = Math.Min(RollingWindowBits, s.Length);
            return 100.0 * Ones(s, s.Length - w) / w;
        }
        // number of 0's after the last 1 (all 0's -> the whole length)
        private static int Trail0(StringBuilder s)
        { int n = 0; for (int i = s.Length - 1; i >= 0 && s[i] == '0'; i--) n++; return n; }

        private static string F(double v) { return v < 0 ? "" : v.ToString("F1", CultureInfo.InvariantCulture); }
        private string PctS(StringBuilder s) { return F(Pct(s)); }
        private string RollS(StringBuilder s) { return F(Roll(s)); }

        // ── alert ────────────────────────────────────────────────────────────
        private DateTime NyNow()
        {
            try
            {
                if (nyZone == null) nyZone = TimeZoneInfo.FindSystemTimeZoneById("Eastern Standard Time");
                return TimeZoneInfo.ConvertTime(DateTime.Now, TimeZoneInfo.Local, nyZone);
            }
            catch { return DateTime.Now; }
        }

        private bool InAlertHours()
        {
            if (!EnableAlertHours) return true;
            DateTime ny = NyNow();
            int now = ny.Hour * 60 + ny.Minute;
            int a = AlertStartHourET * 60 + AlertStartMinuteET, b = AlertEndHourET * 60 + AlertEndMinuteET;
            return a <= b ? (now >= a && now < b) : (now >= a || now < b);
        }

        private void CheckAlert(Setup s, int len0)
        {
            if (s.pa.Count == 0 || s.Deepest.Length <= len0) return;   // only when a NEW bit arrived
            s.AlarmNow = MatchAny(s.Deepest.ToString(), s.pa);
            if (!s.AlarmNow) return;
            s.LastAlert = DateTime.Now; s.AlertCount++;
            bool sound = InAlertHours();
            string msg = s.Label + ": F" + s.Layer + " ends with " + s.PA + " (" + s.LastAlert.ToString("HH:mm:ss") + ")";
            Print("[MultiRecorder] ALERT " + msg + (sound ? "" : "  (outside alert hours: no sound)"));
            Note(s, "# ALERT " + msg + (sound ? "" : " (no sound: outside hours)"));
            if (!sound) return;
            try
            {
                string snd = AlertSoundFile ?? "";
                if (snd.Length > 0 && !Path.IsPathRooted(snd))
                    snd = Path.Combine(NinjaTrader.Core.Globals.InstallDir, "sounds", snd);
                Alert("MultiRecorder_S" + s.N + "_" + s.AlertCount, Priority.High, msg, snd, 0, Brushes.DarkOrange, Brushes.Black);
            }
            catch (Exception ex) { Print("[MultiRecorder] alert error: " + ex.Message); }
        }

        // ── state file for the browser viewer ────────────────────────────────
        private static string J(string s)
        { return "\"" + (s ?? "").Replace("\\", "\\\\").Replace("\"", "\\\"") + "\""; }

        private string LineJson(string name, string pattern, StringBuilder s)
        {
            return "{\"name\":" + J(name) + ",\"pattern\":" + J(pattern) + ",\"bits\":" + s.Length
                 + ",\"ones\":" + Ones(s, 0) + ",\"pct\":" + J(PctS(s)) + ",\"roll\":" + J(RollS(s))
                 + ",\"trail0\":" + Trail0(s) + ",\"s\":" + J(s.ToString()) + "}";
        }

        private void WriteState()
        {
            if (!EnableLog) return;
            try
            {
                var ci = CultureInfo.InvariantCulture;
                var sb = new StringBuilder();
                sb.Append("{\"recorder\":").Append(J(LogBaseName))
                  .Append(",\"written\":").Append(J(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss", ci)))
                  .Append(",\"instrument\":").Append(J(Instrument != null ? Instrument.FullName : ""))
                  .Append(",\"brick\":").Append(J(BarsPeriod != null ? BarsPeriod.ToString() : ""))
                  .Append(",\"last_brick\":").Append(J(Time[0].ToString("yyyy-MM-dd HH:mm:ss", ci)))
                  .Append(",\"since\":").Append(J(since == DateTime.MinValue ? "" : since.ToString("yyyy-MM-dd HH:mm:ss", ci)))
                  .Append(",\"history\":").Append(J(historyNote))
                  .Append(",\"rolling\":").Append(RollingWindowBits)
                  .Append(",\"reset_on_new_session\":").Append(ResetOnNewSession ? "true" : "false")
                  .Append(",\"setups\":[");
                bool first = true;
                foreach (Setup s in setups)
                {
                    if (!first) sb.Append(","); first = false;
                    sb.Append("{\"n\":").Append(s.N).Append(",\"book\":").Append(J(s.Book.ToString()))
                      .Append(",\"on\":").Append(s.On ? "true" : "false").Append(",\"problem\":").Append(J(s.Problem))
                      .Append(",\"f1\":").Append(J(s.P1)).Append(",\"f2\":").Append(J(s.p2.Count > 0 ? s.P2 : ""))
                      .Append(",\"f3\":").Append(J(s.p3.Count > 0 ? s.P3 : "")).Append(",\"alert\":").Append(J(s.PA))
                      .Append(",\"layer\":").Append(s.On ? s.Layer : 0)
                      .Append(",\"money_next\":").Append(s.On && MoneyNext(s) ? "true" : "false")
                      .Append(",\"alarm_now\":").Append(s.AlarmNow ? "true" : "false")
                      .Append(",\"last_alert\":").Append(J(s.LastAlert == DateTime.MinValue ? "" : s.LastAlert.ToString("yyyy-MM-dd HH:mm:ss", ci)))
                      .Append(",\"alert_count\":").Append(s.AlertCount)
                      .Append(",\"lines\":[");
                    if (s.On)
                    {
                        sb.Append(LineJson("F1", s.P1, s.F1));
                        if (s.p2.Count > 0) sb.Append(",").Append(LineJson("F2", s.P2, s.F2));
                        if (s.p3.Count > 0) sb.Append(",").Append(LineJson("F3", s.P3, s.F3));
                    }
                    sb.Append("]}");
                }
                sb.Append("]}");
                string path = Path.Combine(LogFolder, LogBaseName + "_state.json"), tmp = path + ".tmp";
                File.WriteAllText(tmp, sb.ToString());
                if (File.Exists(path)) File.Delete(path);
                File.Move(tmp, path);
            }
            catch { /* the viewer simply keeps the previous state until the next brick */ }
        }

        // ── compact HUD: one line per setup ──────────────────────────────────
        private void UpdateHud()
        {
            var sb = new StringBuilder();
            sb.Append("RENKO MULTI-RECORDER   ").Append(historyNote)
              .Append("   updated ").Append(DateTime.Now.ToString("HH:mm:ss"))
              .Append(Environment.NewLine)
              .Append("(short version: last " + HudTailBits + " bits per setup - full strings in Renko_Recorder_Viewer.html)");
            foreach (Setup s in setups)
            {
                if (!s.On) { if (s.Problem.Length > 0) sb.Append(Environment.NewLine).Append("S" + s.N.ToString("00") + " OFF: " + s.Problem); continue; }
                StringBuilder d = s.Deepest;
                string full = d.ToString();
                int n = Math.Min(Math.Max(0, HudTailBits), full.Length);
                double p = Pct(d), r = Roll(d);
                string line = s.Label.PadRight(30) + "  F" + s.Layer + " bits=" + d.Length
                    + (p >= 0 ? "  " + p.ToString("F1") + "%" : "")
                    + (r >= 0 ? "  last " + Math.Min(RollingWindowBits, d.Length) + ": " + r.ToString("F1") + "%" : "")
                    + (d.Length > 0 ? "  0s in a row: " + Trail0(d) : "")
                    + "  " + (n > 0 ? (full.Length > n ? "..." : "") + full.Substring(full.Length - n) : "-")
                    + (MoneyNext(s) ? "  [MONEY NEXT]" : "")
                    + (s.AlarmNow ? "   <<< ALARM (" + s.PA + ") " + s.LastAlert.ToString("HH:mm:ss")
                       : s.LastAlert != DateTime.MinValue ? "   last alert " + s.LastAlert.ToString("HH:mm:ss") : "");
                sb.Append(Environment.NewLine).Append(line);
            }
            NinjaTrader.NinjaScript.DrawingTools.TextPosition pos =
                HudCorner == RecorderHudCorner.BottomRight ? NinjaTrader.NinjaScript.DrawingTools.TextPosition.BottomRight :
                HudCorner == RecorderHudCorner.BottomLeft  ? NinjaTrader.NinjaScript.DrawingTools.TextPosition.BottomLeft :
                HudCorner == RecorderHudCorner.TopRight    ? NinjaTrader.NinjaScript.DrawingTools.TextPosition.TopRight :
                HudCorner == RecorderHudCorner.Center      ? NinjaTrader.NinjaScript.DrawingTools.TextPosition.Center :
                                                             NinjaTrader.NinjaScript.DrawingTools.TextPosition.TopLeft;
            Draw.TextFixed(this, HUD_TAG + "_" + LogBaseName, sb.ToString(), pos, Brushes.White,
                new SimpleFont("Consolas", BitFontSize), Brushes.DimGray, Brushes.Black, 50);
        }

        // =====================================================================
        // wildcard tail matcher ('?' = one-or-more 1s, '*' = one-or-more 0s) — same as the other indicators
        // =====================================================================
        private static bool MatchAny(string text, List<string> pats)
        {
            for (int i = 0; i < pats.Count; i++) if (TailMatches(text, pats[i])) return true;
            return false;
        }

        private static bool HasWild(string p)
        { for (int i = 0; i < p.Length; i++) if (p[i] == '*' || p[i] == '?') return true; return false; }

        private static bool TailMatches(string text, string pattern)
        {
            if (string.IsNullOrEmpty(pattern) || string.IsNullOrEmpty(text)) return false;
            if (!HasWild(pattern)) return text.Length >= pattern.Length && text.EndsWith(pattern);
            for (int start = text.Length - 1; start >= 0; start--)
                if (MatchHere(text, start, pattern, 0)) return true;
            return false;
        }

        private static bool MatchHere(string text, int ti, string pattern, int pi)
        {
            while (pi < pattern.Length)
            {
                char pc = pattern[pi];
                if (pc == '*' || pc == '?')
                {
                    char want = (pc == '*') ? '0' : '1';
                    if (ti >= text.Length || text[ti] != want) return false;
                    ti++;
                    int maxC = ti;
                    while (maxC < text.Length && text[maxC] == want) maxC++;
                    for (int c = maxC; c >= ti; c--) if (MatchHere(text, c, pattern, pi + 1)) return true;
                    return false;
                }
                else { if (ti >= text.Length || text[ti] != pc) return false; ti++; pi++; }
            }
            return ti == text.Length;
        }

        #region Properties
        // All plain properties (saved with the chart / template; no generated-method parameters).

        [Display(Name="Book", Order=1, GroupName="Setup 01")]
        public RecorderBookType Setup1Book { get; set; }
        [Display(Name="F1 Pattern (blank = setup OFF)", Order=2, GroupName="Setup 01")]
        public string Setup1F1 { get; set; }
        [Display(Name="F2 Pattern (leave blank to skip F2 and F3)", Order=3, GroupName="Setup 01")]
        public string Setup1F2 { get; set; }
        [Display(Name="F3 Pattern (leave blank to skip; needs F2)", Order=4, GroupName="Setup 01")]
        public string Setup1F3 { get; set; }
        [Display(Name="Last Layer Alert Pattern (blank = no alert)", Order=5, GroupName="Setup 01")]
        public string Setup1Alert { get; set; }

        [Display(Name="Book", Order=1, GroupName="Setup 02")]
        public RecorderBookType Setup2Book { get; set; }
        [Display(Name="F1 Pattern (blank = setup OFF)", Order=2, GroupName="Setup 02")]
        public string Setup2F1 { get; set; }
        [Display(Name="F2 Pattern (leave blank to skip F2 and F3)", Order=3, GroupName="Setup 02")]
        public string Setup2F2 { get; set; }
        [Display(Name="F3 Pattern (leave blank to skip; needs F2)", Order=4, GroupName="Setup 02")]
        public string Setup2F3 { get; set; }
        [Display(Name="Last Layer Alert Pattern (blank = no alert)", Order=5, GroupName="Setup 02")]
        public string Setup2Alert { get; set; }

        [Display(Name="Book", Order=1, GroupName="Setup 03")]
        public RecorderBookType Setup3Book { get; set; }
        [Display(Name="F1 Pattern (blank = setup OFF)", Order=2, GroupName="Setup 03")]
        public string Setup3F1 { get; set; }
        [Display(Name="F2 Pattern (leave blank to skip F2 and F3)", Order=3, GroupName="Setup 03")]
        public string Setup3F2 { get; set; }
        [Display(Name="F3 Pattern (leave blank to skip; needs F2)", Order=4, GroupName="Setup 03")]
        public string Setup3F3 { get; set; }
        [Display(Name="Last Layer Alert Pattern (blank = no alert)", Order=5, GroupName="Setup 03")]
        public string Setup3Alert { get; set; }

        [Display(Name="Book", Order=1, GroupName="Setup 04")]
        public RecorderBookType Setup4Book { get; set; }
        [Display(Name="F1 Pattern (blank = setup OFF)", Order=2, GroupName="Setup 04")]
        public string Setup4F1 { get; set; }
        [Display(Name="F2 Pattern (leave blank to skip F2 and F3)", Order=3, GroupName="Setup 04")]
        public string Setup4F2 { get; set; }
        [Display(Name="F3 Pattern (leave blank to skip; needs F2)", Order=4, GroupName="Setup 04")]
        public string Setup4F3 { get; set; }
        [Display(Name="Last Layer Alert Pattern (blank = no alert)", Order=5, GroupName="Setup 04")]
        public string Setup4Alert { get; set; }

        [Display(Name="Book", Order=1, GroupName="Setup 05")]
        public RecorderBookType Setup5Book { get; set; }
        [Display(Name="F1 Pattern (blank = setup OFF)", Order=2, GroupName="Setup 05")]
        public string Setup5F1 { get; set; }
        [Display(Name="F2 Pattern (leave blank to skip F2 and F3)", Order=3, GroupName="Setup 05")]
        public string Setup5F2 { get; set; }
        [Display(Name="F3 Pattern (leave blank to skip; needs F2)", Order=4, GroupName="Setup 05")]
        public string Setup5F3 { get; set; }
        [Display(Name="Last Layer Alert Pattern (blank = no alert)", Order=5, GroupName="Setup 05")]
        public string Setup5Alert { get; set; }

        [Display(Name="Book", Order=1, GroupName="Setup 06")]
        public RecorderBookType Setup6Book { get; set; }
        [Display(Name="F1 Pattern (blank = setup OFF)", Order=2, GroupName="Setup 06")]
        public string Setup6F1 { get; set; }
        [Display(Name="F2 Pattern (leave blank to skip F2 and F3)", Order=3, GroupName="Setup 06")]
        public string Setup6F2 { get; set; }
        [Display(Name="F3 Pattern (leave blank to skip; needs F2)", Order=4, GroupName="Setup 06")]
        public string Setup6F3 { get; set; }
        [Display(Name="Last Layer Alert Pattern (blank = no alert)", Order=5, GroupName="Setup 06")]
        public string Setup6Alert { get; set; }

        [Display(Name="Book", Order=1, GroupName="Setup 07")]
        public RecorderBookType Setup7Book { get; set; }
        [Display(Name="F1 Pattern (blank = setup OFF)", Order=2, GroupName="Setup 07")]
        public string Setup7F1 { get; set; }
        [Display(Name="F2 Pattern (leave blank to skip F2 and F3)", Order=3, GroupName="Setup 07")]
        public string Setup7F2 { get; set; }
        [Display(Name="F3 Pattern (leave blank to skip; needs F2)", Order=4, GroupName="Setup 07")]
        public string Setup7F3 { get; set; }
        [Display(Name="Last Layer Alert Pattern (blank = no alert)", Order=5, GroupName="Setup 07")]
        public string Setup7Alert { get; set; }

        [Display(Name="Book", Order=1, GroupName="Setup 08")]
        public RecorderBookType Setup8Book { get; set; }
        [Display(Name="F1 Pattern (blank = setup OFF)", Order=2, GroupName="Setup 08")]
        public string Setup8F1 { get; set; }
        [Display(Name="F2 Pattern (leave blank to skip F2 and F3)", Order=3, GroupName="Setup 08")]
        public string Setup8F2 { get; set; }
        [Display(Name="F3 Pattern (leave blank to skip; needs F2)", Order=4, GroupName="Setup 08")]
        public string Setup8F3 { get; set; }
        [Display(Name="Last Layer Alert Pattern (blank = no alert)", Order=5, GroupName="Setup 08")]
        public string Setup8Alert { get; set; }

        [Display(Name="Book", Order=1, GroupName="Setup 09")]
        public RecorderBookType Setup9Book { get; set; }
        [Display(Name="F1 Pattern (blank = setup OFF)", Order=2, GroupName="Setup 09")]
        public string Setup9F1 { get; set; }
        [Display(Name="F2 Pattern (leave blank to skip F2 and F3)", Order=3, GroupName="Setup 09")]
        public string Setup9F2 { get; set; }
        [Display(Name="F3 Pattern (leave blank to skip; needs F2)", Order=4, GroupName="Setup 09")]
        public string Setup9F3 { get; set; }
        [Display(Name="Last Layer Alert Pattern (blank = no alert)", Order=5, GroupName="Setup 09")]
        public string Setup9Alert { get; set; }

        [Display(Name="Book", Order=1, GroupName="Setup 10")]
        public RecorderBookType Setup10Book { get; set; }
        [Display(Name="F1 Pattern (blank = setup OFF)", Order=2, GroupName="Setup 10")]
        public string Setup10F1 { get; set; }
        [Display(Name="F2 Pattern (leave blank to skip F2 and F3)", Order=3, GroupName="Setup 10")]
        public string Setup10F2 { get; set; }
        [Display(Name="F3 Pattern (leave blank to skip; needs F2)", Order=4, GroupName="Setup 10")]
        public string Setup10F3 { get; set; }
        [Display(Name="Last Layer Alert Pattern (blank = no alert)", Order=5, GroupName="Setup 10")]
        public string Setup10Alert { get; set; }

        [Display(Name="Include back fill (replay stored bricks on enable)", Order=1, GroupName="Common 1. History")]
        public bool IncludeBackFill { get; set; }

        [Range(1, int.MaxValue)]
        [Display(Name="Back-fill max bricks", Order=2, GroupName="Common 1. History")]
        public int BackFillMaxBricks { get; set; }

        [Display(Name="Back fill may cross the session start", Order=3, GroupName="Common 1. History",
            Description="OFF (default) = replay stops at this session's first brick. ON = replay goes further back, up to the max bricks.")]
        public bool BackFillCrossSession { get; set; }

        [Display(Name="Reset strings on new session", Order=4, GroupName="Common 1. History",
            Description="ON (default) = all strings start empty at each new session. OFF = they continue across sessions.")]
        public bool ResetOnNewSession { get; set; }

        [Display(Name="Show HUD (one line per setup)", Order=1, GroupName="Common 2. Display")]
        public bool ShowHud { get; set; }

        [Range(0, 500)]
        [Display(Name="HUD: last N bits per line", Order=2, GroupName="Common 2. Display")]
        public int HudTailBits { get; set; }

        [Range(0, 1000)]
        [Display(Name="Rolling window (bits, 0 = hide)", Order=3, GroupName="Common 2. Display")]
        public int RollingWindowBits { get; set; }

        [Range(6, 72)]
        [Display(Name="HUD font size", Order=4, GroupName="Common 2. Display")]
        public int BitFontSize { get; set; }

        [Display(Name="HUD corner", Order=5, GroupName="Common 2. Display")]
        public RecorderHudCorner HudCorner { get; set; }

        [Display(Name="Enable logs + state file (needed by the browser viewer)", Order=1, GroupName="Common 3. Files")]
        public bool EnableLog { get; set; }

        [Display(Name="Log Folder", Order=2, GroupName="Common 3. Files")]
        public string LogFolder { get; set; }

        [Display(Name="Log Base Name (use a different one per chart)", Order=3, GroupName="Common 3. Files")]
        public string LogBaseName { get; set; }

        [Display(Name="Alert sound file (in NinjaTrader 'sounds' folder, or full path; blank = silent)", Order=1, GroupName="Common 4. Alert")]
        public string AlertSoundFile { get; set; }

        [Display(Name="Use alert hours (no sound outside; text still updates)", Order=2, GroupName="Common 4. Alert")]
        public bool EnableAlertHours { get; set; }

        [Range(0, 23)]
        [Display(Name="Alert start hour (New York time, ET)", Order=3, GroupName="Common 4. Alert", Description="Default 9:30 ET = 6:30 AM Pacific.")]
        public int AlertStartHourET { get; set; }

        [Range(0, 59)]
        [Display(Name="Alert start minute (ET)", Order=4, GroupName="Common 4. Alert")]
        public int AlertStartMinuteET { get; set; }

        [Range(0, 23)]
        [Display(Name="Alert end hour (New York time, ET)", Order=5, GroupName="Common 4. Alert", Description="Default 16:00 ET = 1:00 PM Pacific.")]
        public int AlertEndHourET { get; set; }

        [Range(0, 59)]
        [Display(Name="Alert end minute (ET)", Order=6, GroupName="Common 4. Alert")]
        public int AlertEndMinuteET { get; set; }
        #endregion
    }
}

#region NinjaScript generated code. Neither change nor remove.

namespace NinjaTrader.NinjaScript.Indicators
{
	public partial class Indicator : NinjaTrader.Gui.NinjaScript.IndicatorRenderBase
	{
		private Indicator_Renko_MultiRecorder[] cacheIndicator_Renko_MultiRecorder;
		public Indicator_Renko_MultiRecorder Indicator_Renko_MultiRecorder()
		{
			return Indicator_Renko_MultiRecorder(Input);
		}

		public Indicator_Renko_MultiRecorder Indicator_Renko_MultiRecorder(ISeries<double> input)
		{
			if (cacheIndicator_Renko_MultiRecorder != null)
				for (int idx = 0; idx < cacheIndicator_Renko_MultiRecorder.Length; idx++)
					if (cacheIndicator_Renko_MultiRecorder[idx] != null &&  cacheIndicator_Renko_MultiRecorder[idx].EqualsInput(input))
						return cacheIndicator_Renko_MultiRecorder[idx];
			return CacheIndicator<Indicator_Renko_MultiRecorder>(new Indicator_Renko_MultiRecorder(), input, ref cacheIndicator_Renko_MultiRecorder);
		}
	}
}

namespace NinjaTrader.NinjaScript.MarketAnalyzerColumns
{
	public partial class MarketAnalyzerColumn : MarketAnalyzerColumnBase
	{
		public Indicators.Indicator_Renko_MultiRecorder Indicator_Renko_MultiRecorder()
		{
			return indicator.Indicator_Renko_MultiRecorder(Input);
		}

		public Indicators.Indicator_Renko_MultiRecorder Indicator_Renko_MultiRecorder(ISeries<double> input )
		{
			return indicator.Indicator_Renko_MultiRecorder(input);
		}
	}
}

namespace NinjaTrader.NinjaScript.Strategies
{
	public partial class Strategy : NinjaTrader.Gui.NinjaScript.StrategyRenderBase
	{
		public Indicators.Indicator_Renko_MultiRecorder Indicator_Renko_MultiRecorder()
		{
			return indicator.Indicator_Renko_MultiRecorder(Input);
		}

		public Indicators.Indicator_Renko_MultiRecorder Indicator_Renko_MultiRecorder(ISeries<double> input )
		{
			return indicator.Indicator_Renko_MultiRecorder(input);
		}
	}
}

#endregion
