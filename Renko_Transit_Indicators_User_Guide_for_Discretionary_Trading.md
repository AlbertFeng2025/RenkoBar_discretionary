# Renko Transit Indicators — User Guide for Discretionary Trading

|  |  |  |
| --- | --- | --- |
|  |  |  |
|  |  |  |

Oct 27, 2026 · @Albert Feng · updated Nov 17, 2026 (Layer 4; fast-market fills from live tests)

## Purpose and honest expectations

These indicators are observers: they never place an order. They turn the Renko brick stream into 1/0 outcome strings (1 = the transit would have won, 0 = it missed) and show them layer by layer, so you can see the current rhythm of a setup and decide by judgment whether and when to trade.

What the research says, so the tools are used with the right expectations:

- Across Jul–Oct and Apr–May 2026 MNQ data, the outcome strings behave close to a coin flip at the payoff's break-even rate. No F1/F2/F3 rule beat costs reliably on new data.
- A pattern such as `1000` (a win, then three losses) does not by itself predict the next bit. The alert means "look now", not "trade now".
- Your judgment (news, the open, the day's direction, how price is moving right now) is the part that might add an edge. The indicators help you find the moments to apply it and keep an honest record.
- Measure your discretionary trades against the setup's base win rate (about 33% for a 2-brick target, 25% for 3 bricks; about 67% for a continuation setup with a 1-brick target and 2-brick stop), never against 50%.

## The tools and when to use each

Use the multi-recorder plus the browser viewer for daily watching; use a single indicator when you want one setup drawn large on the chart.

| Tool | What it does | Use it when |
| --- | --- | --- |
| Indicator\_Renko\_MultiRecorder | Up to 10 setups in one indicator, each with F1–F4 (Layer 4 from v1.6); one HUD line each; per-setup CSV logs; state file for the viewer; last-layer alerts | Every day: watching several books and F1 patterns at once |
| Renko\_Recorder\_Viewer.html | Opens in Chrome or Edge, reads the recorder's state file every 1.5 s, shows every string in full with percentages, 0s in a row and alerts | On a second monitor, or whenever the HUD is too short |
| Indicator\_Renko\_mergedLayer3 | One MERGED setup (F1, F2, F3) drawn on its own panel, LIVE and BACKFILL blocks | A close look at one merged setup |
| Indicator\_Renko\_LONGLayer3 | Same for the LONG book (green brick = 1) | A close look at one LONG setup |
| Indicator\_Renko\_SHORTLayer3 | Same for the SHORT book (red brick = 1) | A close look at one SHORT setup |

All five follow the same string rules, so a string read in the viewer equals the one a single indicator shows for the same setup and settings. The three single indicators stop at Layer 3; to watch a Layer 4 setup, use a multi-recorder slot with F4 set.

## Indicators and the strategy files

The trading strategies come as one file per layer; the indicators are one file each, and you choose the layer by leaving patterns blank.

|  | Strategy files (trade) | Indicators (observe) |
| --- | --- | --- |
| MERGED | `Renko_mergedTransit_Layer1`, `_Layer2`, `_Layer3`, `_layer4` — a separate file per layer | `Indicator_Renko_mergedLayer3` (or a MERGED slot in the multi-recorder) |
| LONG / SHORT | Single-book files per layer (Layer 1, Layer 2, Layer 3, Layer 4) | `Indicator_Renko_LONGLayer3` / `_SHORTLayer3` (or a LONG / SHORT slot) |
| How the layer is chosen | By which file you enable, then by blanks: in a Layer 3 file F3 blank = Layer 2; in a Layer 4 file F4 blank = Layer 3 and F3 blank = Layer 2. A blank F2 in a strategy means never trade (unlike the indicators) — so no Layer 2/3/4 file can run Layer 1 | F2 blank = Layer 1; F3 blank = Layer 2; F4 blank = Layer 3; all four set = Layer 4. F3 needs F2 and F4 needs F3 |

**To make the indicator show exactly what a strategy holds:**

1. Same book and the same F1 / F2 / F3 / F4 patterns; leave blank every layer the strategy does not use (a Layer 2 strategy = F3 and F4 blank; a Layer 3 strategy = F4 blank).
2. Same brick size on the same instrument.
3. Same history settings: back fill on enable and the session reset ("Restart clean on new session" / "Reset strings on new session").

Then the indicator's deepest string matches the strategy's theoretical money string (its filter result) bit for bit, and its `MONEY NEXT` tag shows when the strategy would place its next real order (subject to the strategy's own trading hours, open-position and safety rules).

## Theory string vs. real money string

The last-layer money line in the indicators and the viewer is a theoretical filter result; the qty rule, exit pattern and breaker of a strategy read a different string — the real money trades.

| String | What it holds | Where you see it | Read by |
| --- | --- | --- | --- |
| **Filtered (theory) money string** — the deepest line, e.g. `F2 = money` | Every signal that passed F1 / F2 (/ F3), scored by the next brick (1 = would-be win), whether or not a trade happened | Indicators, the viewer, the recorder's CSV logs. In the strategy logs mostly **not**: the LONG/SHORT Layer 2 CSV has no such column, and in the LONG/SHORT Layer 3 file the `filter2Outcome` column stays empty when F3 is blank; only the merged files print it (`f2out` lines in the log) | You, to judge the setup; F3 (if set) |
| **Real money string** — `realTradeOutcome` in the strategy log | Only the trades the strategy actually filled: 1 = more contracts hit the target than the stop, 0 = otherwise | Strategy CSV / log, `real=` on each `TRADE CLOSED` line | **Qty rule** and **exit pattern** (and the **breaker**, which counts its 0's in a row) |

**Which strings each strategy log shows.** The qty rule, exit pattern and breaker use **only the real money string** in all three strategies; the theory strings in a log are there for you to read (and, when F3 is set, to arm F3).

| String | Merged log | LONG / SHORT log |
| --- | --- | --- |
| F1 outcome (theory, Layer 1) | yes — `f1out=` | yes — column `filter1Outcome` |
| Last-layer outcome (theory, F2) | **yes** — `f2out=` | **only Layer 3 with F3 set** (`filter2Outcome`); Layer 2, or Layer 3 with F3 blank: **no** |
| Layer 3 outcome (theory, F3) — Layer 4 files | **only with F4 set** — `f3out=` | **only with F4 set** — column `filter3Outcome` (last column) |
| Real money trades — **used by the qty rule, exit pattern, breaker** | yes — `real=` on each `TRADE CLOSED`, `[QTY] sessionReal=` | yes — column `realTradeOutcome` |

**Log files.** The merged strategy writes **one** file — its log is the diagnostic log (bricks, signals, `[F2 MATCH]`, `MONEY TRADE`, `TRADE CLOSED`); there is no separate `-diagLog` file for merged, by design. LONG / SHORT write two files: the trade CSV and `<name>-diagLog.csv`.

**When the two agree.** Only when every signal the filter allows really becomes a filled trade, and the stop and target match the brick geometry: transit (F1 ends in 0) = stop 1 brick, target 2 bricks; continuation (F1 ends in 1) = stop 2 bricks, target 1 brick.

**Why they differ in real trading.**

- **Signals not traded** (the theory string gets an extra bit): a position still open (busy), outside trading hours, margin cutoff, a trend/speed gate, the qty rule returned 0, the strategy already stopped by the exit pattern or breaker, real orders off, or the strategy enabled after the signal brick closed.
- **Fills:** the target touched but not filled and the trade later lost (theory 1, real 0); several contracts with more stopped than filled at the target (majority rule, real 0); stop slippage costs money but does not change the bit.
- **Different start:** the viewer includes history from before the strategy was enabled.
- **Time window:** the qty rule reads today's real trades in all files; the exit pattern reads today's real trades in LONG/SHORT, but every real trade since enable in the merged files.

**In practice.** Use the theory line to decide when to run a strategy; judge your money, size and stops by the real string. A run of 0's on the viewer is not the same as real losses in a row — the qty rule and breaker count only the real ones. The strategy log says why a signal was not traded (`[OUTSIDE HOURS]`, `skipped (busy)`, `[QTY SKIP]`) and how each trade scored (`winQty` / `lossQty` on `TRADE CLOSED`).

## How the strings are built

Each layer filters the one below it, and the deepest active layer is the would-be money string.

&#91;embedded content: layer chain · bricks to F1, F2, F3\]

**Brick bits.** Every closed brick becomes one bit: green (close rose) = 1, red (close fell) = 0.

**Books.**

- **LONG:** bricks as they are (green = 1). After an F1 match, the next brick green = `1` (win), red = `0`.
- **SHORT:** bricks flipped (red = 1). After an F1 match, the next brick red = `1`.
- **MERGED:** F1 is tested on both strings each brick; LONG and SHORT outcomes go into one string in time order.

**Layers.**

1. **F1** (on the brick string): each time the brick string ends with the F1 pattern, the next brick is scored and its 1/0 is added to the F1 string. This is Layer 1.
2. **F2** (on the F1 string): each time the F1 string ends with the F2 pattern, the next F1 outcome is copied into the F2 string. This is Layer 2.
3. **F3** (on the F2 string): each time the F2 string ends with the F3 pattern, the next F2 outcome is copied into the F3 string. This is Layer 3. F3 needs F2: with F2 blank, F3 is ignored.
4. **F4** (on the F3 string): each time the F3 string ends with the F4 pattern, the next F3 outcome is copied into the F4 string. This is Layer 4. F4 needs F3: with F3 blank, F4 is ignored (the setup shows a note).

**MONEY NEXT** means F2 (and F3, F4, if set) is armed: the next F1 transit would be a real trade under that pipeline.

**An armed layer stays armed until its own input string changes.** F4 changes only when a new F3 outcome arrives, so F4 can stay armed while F2 or F3 switch off and on again (the same way F3 stays armed while F2 is off). Live example: F4 armed at 10:50, F3 went off and on, and the money trade came at 11:25.

**Pattern rules.** `0` and `1` match themselves; `?` = one or more 1s; `*` = one or more 0s; a comma means OR (`01,10` = either). Patterns match the end of the string.

**Worked example** (MERGED, F1 = `10`, F2 = `01,10`): a green brick then a red brick makes the LONG string end in `10`, so the next brick is scored for LONG. If the F1 string now ends in `01` or `10`, the next F1 outcome also goes into F2. The F2 string is what a Layer 2 trader would have traded.

## Reading the screen

Read the deepest (money) line first: its rolling percentage and its 0s in a row tell you the setup's current state at a glance.

A viewer line looks like this:

`F2 = money (01,10) bits 50 · 1s 18 = 36.0% · last 20: 35.0% · 0s in a row: 3`

| Item | Meaning |
| --- | --- |
| `F2 = money (01,10)` | The layer and its pattern; "= money" marks the deepest active layer |
| `bits 50` | How many outcomes the string holds (this session, or since back fill) |
| `1s 18 = 36.0%` | Wins so far and their share: the session win rate |
| `last 20: 35.0%` | Win rate over the most recent 20 bits (the rolling window), like an RSI for the string |
| `0s in a row: 3` | 0's after the last 1 of the deepest string: the current losing run |
| `MONEY NEXT` | F2 (and F3, F4, if set) armed: the next F1 transit would be a trade under this pipeline |
| `ALARMING NOW 10:42:13` | The deepest string just ended with the alert pattern (default `1000`); one sound was played |
| `last alert 10:42:13` | When the alert last fired; stays so you can read it later |

In the viewer, 1s are green and grouped by 10 counted from the newest bit, an alarming setup gets an orange frame, running recorders carry a green LIVE badge and sit on top, and recorders that are not running (removed, reloaded, NinjaTrader closed, or no ticks for about 75 seconds, e.g. the daily break) are folded at the bottom with the reason. Each header also shows the update number (the same #N as the HUD line "state file … #N") and a market-speed row (FAST / NORMAL / SLOW / SUPER SLOW, average seconds per brick, and how long the current brick has been open).

The chart HUD shows the same items on one short line per setup (last 30 bits).

## Trading workflow: Layer 1, 2, 3 and 4

The trade itself is always the same: when the F1 pattern forms on the chart, enter at that brick's close in the book's direction, with a stop 1 brick away and a target 2 bricks away (or your chosen geometry). The layers only decide which of those transits you take.

**Which direction?** LONG book = buy, SHORT book = sell. In MERGED, read the last bricks on the chart: with F1 = `10`, green then red is a LONG transit (buy, chase the next green); red then green is a SHORT transit (sell, chase the next red).

**Choosing the book.** MERGED trades both sides and suits a two-way, choppy session. LONG or SHORT alone suits a session where you already have a view of direction (a trend day, a gap you expect to hold or fade). That view is your discretionary input.

### Layer 1 (F1 only)

1. Every F1 match is a candidate trade, so trades are frequent.
2. Use Layer 1 mainly to read the rhythm: is the rolling win rate running above or below its usual level?
3. If you trade it, pick moments with your own context (time of day, momentum) rather than every match.

### Layer 2 (F1 + F2, the default: F2 = `01,10`)

1. Watch the F2 = money line and its `MONEY NEXT` tag.
2. When `MONEY NEXT` shows, the next F1 transit is a Layer 2 trade: wait for the F1 pattern to form on the chart.
3. Enter at that brick's close in the book's direction; set stop and target at once.
4. With a 1-brick stop and 2-brick target the very next brick decides it: a brick your way hits the target, a brick against you hits the stop.

### Layer 3 (F1 + F2 + F3)

1. Same as Layer 2, but `MONEY NEXT` needs both F2 and F3 armed, so trades are rarer.
2. Set F3 in the recorder when you want a fixed rule for the extra wait, e.g. F3 = `0` (take the next F2 transit only after an F2 loss).

### Layer 4 (F1 + F2 + F3 + F4)

1. Same again, one level deeper: `MONEY NEXT` needs F2, F3 **and** F4 armed. While F4 is not armed, a signal with F2 and F3 armed is only observed (strategy log: `L3 obs only (F4 not armed)`) and its result goes into the F3 string.
2. Trades become much rarer. A Layer 4 rule built from 0's on a continuation setup (about 67% 1's) can go a whole session without arming — in the Nov 16–17 SHORT test (F1 `001`, F2 `01,10`, F3 `10`, F4 `0`), F2 and F3 were armed together only once, that signal won, so F4 = `0` never armed and there was no trade. That was correct, just rare.
3. A deeper layer does not create an edge (see the research above); it only picks which of the same transits you take.

**One Layer 4 file covers Layer 4, Layer 3 and Layer 2 — not Layer 1.**

| In a Layer 4 strategy file | Behaves as |
| --- | --- |
| F1, F2, F3, F4 set | Layer 4 |
| F4 blank | Layer 3 — identical to the Layer 3 file (checked bit for bit) |
| F3 blank | Layer 2 — F4 is ignored even if typed (`[F4] IGNORED …` in the log) |
| F2 blank | never trades — this is **not** Layer 1; use the Layer 1 file |

Check the ready line at the top of the log (`F2=[…], F3=[…], F4=[…]`) to confirm which layer is running. The Layer 3 files work the same way one level up (F3 blank = Layer 2, never Layer 1).

### Using the alert and "0s in a row" (manual Layer 3)

The default alert `1000` fires when the money string shows a win followed by three losses. That is your cue to look, not an order.

1. Alert sounds: check the setup's line and the chart.
2. Decide by judgment: trade the next money transit now, or wait for more losses.
3. If you wait, watch `0s in a row` climb (4, 5, 6 …). Waiting for, say, 6 zeros is the same idea as a Layer 3 rule of `000000`, judged by eye instead of fixed in settings.
4. When you act, the trade is the next money transit (step 2 of Layer 2).

Keep in mind: the research found outcomes close to independent, so a long losing run does not make the next bit more likely to win. Waiting mainly reduces how often you trade; runs of 10 or more 0s do happen. Size every trade as if the run can continue.

### Example

MERGED, F1 = `10`, F2 = `01,10`, alert `1000`. At 10:42 the alert sounds: F2 = money ends `…1000`, `0s in a row: 3`. You decide to wait. Two more F2 outcomes come in as 0 (`0s in a row: 5`). The market looks stretched to you, `MONEY NEXT` is showing, and a green brick follows a red one: a SHORT transit. You sell at that brick's close, stop 1 brick above, target 2 bricks below.

## Why intraday Renko is hard to automate, and a semi-auto middle way

No Renko rule we tested held up on new data, so full automation is not justified; a semi-automatic approach keeps your judgment for when to run and lets the strategy handle execution and the stop rules.

**Why a fixed rule struggles intraday**

- **Brick colors are close to a coin flip.** With a 1-brick stop, the win rate sits where the payoff says it should (about 33% for 2 bricks, 25% for 3), leaving nothing to cover costs.
- **Costs are large next to small bricks.** About 0.5–1 point per MNQ trade is a big share of a 10-point brick.
- **Results depend on the market regime.** The best Jul–Oct setup lost 6.5 points per trade in the Apr–May rally.
- **Results depend on where the brick grid starts.** Shifting the same 80-tick grid by a few points moved one setup from −0.4 to +3.3 points per trade.
- **Picking the best of many tests flatters it.** After 100+ variants, the best looks good by chance alone.
- **Outcome filters add little.** Wins and losses are close to independent, so F2 and F3 rarely move the win rate.

**The semi-auto approach**

You decide when the market suits the setup; the strategy file does the rest by rule.

1. **Choose the moment.** Use the indicators, the alert and your read of the session (time of day, direction, volatility) to decide that this setup is worth running now.
2. **Enable the matching strategy file** with the same book and patterns you were watching (Layer 1, 2 or 3 file), flat size, stop and target in the strategy.
3. **Hand off.** The strategy takes every money transit by its rules, with a server-side bracket on each entry and its safety guards (naked-position guard, fast-market rescue, margin-cutoff flatten).
4. **Let the exit rules end the run.** Set them before you enable, so the strategy switches itself off:
   - **Trade-outcome exit pattern** on the real money string (today's real trades in LONG/SHORT; every real trade since enable in the merged files), e.g. `111` = stop after three real wins in a row (lock in a good run).
   - **Max real losses in a row**, e.g. 3 = stop after three straight real losses (cap the bad run).
   - **Trading hours / strategy life** so it never runs into hours you did not choose.
5. **Review afterwards** from the strategy's log: trades, win rate against the base rate, and which exit rule ended it.

What this does and does not do: it removes in-the-moment hesitation and caps the damage of a bad stretch, but it does not create an edge. Any edge still has to come from your choice of when to run it, so keep score honestly. Also check the platform's connection-loss setting and glance at the strategy now and then: a long disconnect can still leave a position unprotected.

## Fast markets and fills — what the live tests showed

Five merged test trades on Nov 17, 2026 (MNQ 40-tick, stop 10 / target 20) were checked fill by fill. The code made every decision correctly; the money lost against perfect fills came from market speed.

**How a strategy places the bracket**

- Entry is a **market** order sent right after the signal brick closes.
- Stop and target are measured from the **brick close** (`brickClose` on the `MONEY TRADE` line), not from your fill price.
- The **target is a limit** order: it fills at its own price (slightly better only if price jumps over it). The **stop is a stop-market** order: it can slip.

**What happened**

| | Effect (points × contracts) |
| --- | --- |
| Entry slippage, 5 trades | **−1,548** — two entries in very fast bursts were 5.75 and 6 points late (−770 at 154 contracts, −528 at 88) |
| Exit slippage, 5 trades | **+173** — targets sometimes filled past their price |
| Total vs. perfect fills | **−1,375** (about −$2,750 on MNQ) |

**Why a fast move did not pay more.** The move happened while the entry was filling, so the entry was late; the target stayed at brick close ± 20, and a limit target pays only its own price. Example: SHORT, brick close 27022.75, filled at 27016.75; target 27002.75 → +14 instead of +20, and the stop was 16 points away instead of 10. In a fast market entries and stops can slip against you, but a fixed target almost never pays more. For continuation (1-brick target, 2-brick stop) a late entry hurts even more — see the Continuation Guide, section 12.

**Other things seen (all normal)**

- Time from brick close to the order being sent was usually 0.1–0.2 s, but 1.2 s and 4.2 s were seen (time spent inside NinjaTrader placing the order). It was not the main cause: a 0.2 s trade still slipped 5.75 points, and the 4.2 s trade filled 1.25 points better. If long gaps are frequent, check the connection and the PC's load.
- `[RECONCILE] flat & idle but stray order(s) live -> canceling, skipping entry this brick` — a safety check after a stop filled during a burst of 3 bricks in 0.1 s. It cancelled a leftover order; that brick had no signal.
- Slippage is paid per contract: a qty rule that raises size after losses also raises the cost of each late fill.
- **To check a trade:** compare `brickClose` on the `MONEY TRADE` line with `entry=` on its `TRADE CLOSED` line.

## Risk and discipline

Trade flat size, put the stop in with every entry, and judge yourself against the base win rate over at least a few hundred trades.

| Geometry | Break-even win rate (before costs) | Notes |
| --- | --- | --- |
| Stop 1 brick, target 2 bricks | 33.3% | The next brick decides the trade |
| Stop 1 brick, target 3 bricks | 25.0% | Needs two bricks your way before one against |
| Stop 2 bricks, target 1 brick (continuation: F1 ends in 1) | 66.7% | The next brick decides the trade; see the separate Renko Continuation Guide — by Current Code |

Costs (commission plus slippage, about 0.5–1 point per MNQ trade) raise these: a 2-brick target on 40-tick bricks needs about 38% to break even.

- **Stop with every entry.** Place the stop and target as one bracket the moment you enter.
- **Flat size.** Increasing size after losses (the qty rule) made losing days much worse in testing: one Sim day went from about −$25,500 at flat size to −$46,000 at ×3.
- **Expect long losing runs.** Runs of 10–20 losses in a row appeared in the research; size so that a run of 20 is survivable.
- **Daily stop.** Decide before the open how many losses or points end your day.
- **Bigger bricks cost less.** On 40-tick bricks, costs take a large share of every trade; 80-tick and larger bricks lose less to costs.
- **Keep a record.** For each trade: time, setup, layer, why you took it (alert, 0s in a row, context), result. The recorder's CSV logs hold the strings; add your own trade notes.
- **Review weekly.** If your discretionary trades are not beating the base win rate after a few hundred trades, your judgment is not adding an edge yet.

## Setup and settings reference

The multi-recorder works out of the box: add it to the Renko chart, click OK, open the viewer and choose `C:\temp`.

**Daily start**

1. Open the MNQ Renko chart (e.g. 40-tick or 80-tick) with the multi-recorder on it.
2. Open `Renko_Recorder_Viewer.html` (kept in `Documents\NinjaTrader 8\RenkoTools\`) in Chrome or Edge and click Reconnect.
3. Check that the LIVE section header shows the exact Log Base Name you gave this run (also the first line of the chart HUD and the chart label "Recorder: name"), then its instrument, brick size, last brick time and back fill note.
4. Check the NinjaScript Output window or `<LogBaseName>_<chart>_setup_check.txt` for any setup turned OFF by a typo.

**Default setups** (all with F2 = `01,10`, F3 and F4 blank, alert `1000`)

| Setups | Book | F1 |
| --- | --- | --- |
| 1–3 | MERGED | 10, 100, 1000 |
| 4–6 | LONG | 10, 100, 1000 |
| 7–9 | SHORT | 10, 100, 1000 |
| 10 | (blank, off) | — |

**Common settings**

| Setting | Default | What it does |
| --- | --- | --- |
| Include back fill | ON | Replays stored bricks on enable, so strings start warm |
| Back-fill max bricks | 1000 | Most bricks replayed |
| Back fill may cross the session start | OFF | ON = replay goes past today's session start |
| Reset strings on new session | ON | OFF = strings continue across sessions |
| Show HUD / last N bits / font | ON / 30 / 12 | The short on-chart version |
| Rolling window | 20 bits | Sets the "last N" percentage |
| Log Folder / Log Base Name | `C:\temp` / `Renko_MultiRecorder` | **A new unique name every time** you add or reload the indicator, one per chart: instrument-date-time, e.g. `MNQ10-28-2026-1PM` (two charts: `MNQ40-10-28-2026-1PM`, `MNQ80-10-28-2026-1PM`). It heads the LIVE section in the viewer, the first line of the HUD and the chart label |
| Alert sound file | `Alert2.wav` | In NinjaTrader's sounds folder; blank = silent |
| Alert hours (New York time, ET) | 9:30–16:00 ET (6:30 AM–1:00 PM PT) | No sound outside; the text still updates |

**Files in the log folder**

- `<LogBaseName>_<chart>_S01_MERGED_F1-10_F2-01_10_F3-off_<date-time>.csv` — one per setup per enable: every outcome with full strings (back-fill rows marked `replay=1`), resets and alerts noted. With F4 set the name also gets `_F4-…`, and the columns `inF4, f4_pct, f4_roll, f4` are added at the end (older columns keep their place).
- `<LogBaseName>_state.json` — the live state, rewritten after every brick; read by the viewer.
- `<LogBaseName>_<chart>_setup_check.txt` — how each slot was understood.

**Troubleshooting**

| Symptom | Check |
| --- | --- |
| Viewer shows nothing | Same folder as the recorder's Log Folder; "Enable logs" ON; Chrome or Edge, not Firefox |
| Viewer jumps between two charts' data | Two recorders share a Log Base Name: give each run and each chart its own new name |
| Recorder shows NOT RUNNING | "stopped at" = removed, reloaded or NinjaTrader closed; "no update for" = no ticks (daily break, disconnect) — it turns LIVE again by itself. An old run's \_state.json can be deleted |
| A setup shows OFF | A typo in a pattern: only `0 1 * ?` and commas are allowed |
| Setup note "F3 is blank -> F4 ignored" (or "F2 is blank -> F3 and F4 ignored") | A deeper layer needs the one above it: fill in the missing layer, or clear the deeper one |
| Trade made less than the target / lost more than the stop | Usually entry or stop slippage in a fast market — see "Fast markets and fills" |
| No alert sound | Outside alert hours, sound file blank or missing, or Windows volume |
| New defaults not showing | Remove and re-add the indicator; re-save any old template |
| HUD: "!!!! NAME ALREADY USED" | Another chart (other instrument or brick size) already runs a recorder with this name; this chart stays out of the viewer until it gets its own name |
| HUD: "!! STATE FILE NOT WRITTEN" | The Log Folder is missing or locked; the reason is on the HUD and in the NinjaScript Output window |
| Viewer money line differs from the strategy's real results | Expected at times: see "Theory string vs. real money string" (skipped signals, fills, start time) |
