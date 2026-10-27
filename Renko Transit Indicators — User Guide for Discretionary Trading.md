# Renko Transit Indicators — User Guide for Discretionary Trading

Oct 27, 2026 · @Albert Feng

## Purpose and honest expectations

These indicators are observers: they never place an order. They turn the Renko brick stream into 1/0 outcome strings (1 = the transit would have won, 0 = it missed) and show them layer by layer, so you can see the current rhythm of a setup and decide by judgment whether and when to trade.

What the research says, so the tools are used with the right expectations:

- Across Jul–Oct and Apr–May 2026 MNQ data, the outcome strings behave close to a coin flip at the payoff's break-even rate. No F1/F2/F3 rule beat costs reliably on new data.
- A pattern such as `1000` (a win, then three losses) does not by itself predict the next bit. The alert means "look now", not "trade now".
- Your judgment (news, the open, the day's direction, how price is moving right now) is the part that might add an edge. The indicators help you find the moments to apply it and keep an honest record.
- Measure your discretionary trades against the setup's base win rate (about 33% for a 2-brick target, 25% for 3 bricks), never against 50%.

## The tools and when to use each

Use the multi-recorder plus the browser viewer for daily watching; use a single indicator when you want one setup drawn large on the chart.

| Tool | What it does | Use it when |
| --- | --- | --- |
| Indicator\_Renko\_MultiRecorder | Up to 10 setups in one indicator; one HUD line each; per-setup CSV logs; state file for the viewer; last-layer alerts | Every day: watching several books and F1 patterns at once |
| Renko\_Recorder\_Viewer.html | Opens in Chrome or Edge, reads the recorder's state file every 1.5 s, shows every string in full with percentages, 0s in a row and alerts | On a second monitor, or whenever the HUD is too short |
| Indicator\_Renko\_mergedLayer3 | One MERGED setup (F1, F2, F3) drawn on its own panel, LIVE and BACKFILL blocks | A close look at one merged setup |
| Indicator\_Renko\_LONGLayer3 | Same for the LONG book (green brick = 1) | A close look at one LONG setup |
| Indicator\_Renko\_SHORTLayer3 | Same for the SHORT book (red brick = 1) | A close look at one SHORT setup |

All five follow the same string rules, so a string read in the viewer equals the one a single indicator shows for the same setup and settings.

## Indicators and the strategy files

The trading strategies come as one file per layer; the indicators are one file each, and you choose the layer by leaving patterns blank.

|  | Strategy files (trade) | Indicators (observe) |
| --- | --- | --- |
| MERGED | `Renko_mergedTransit_Layer1`, `_Layer2`, `_Layer3` — a separate file per layer | `Indicator_Renko_mergedLayer3` (or a MERGED slot in the multi-recorder) |
| LONG / SHORT | Single-book files per layer (Layer 1, Layer 2) | `Indicator_Renko_LONGLayer3` / `_SHORTLayer3` (or a LONG / SHORT slot) |
| How the layer is chosen | By which file you enable | F2 and F3 blank = Layer 1; F3 blank = Layer 2; all three set = Layer 3 |

**To make the indicator show exactly what a strategy holds:**

1. Same book and the same F1 / F2 / F3 patterns; leave blank every layer the strategy file does not have (a Layer 2 strategy = F3 blank).
2. Same brick size on the same instrument.
3. Same history settings: back fill on enable and the session reset ("Restart clean on new session" / "Reset strings on new session").

Then the indicator's deepest string matches the strategy's money string bit for bit, and its `MONEY NEXT` tag shows when the strategy would place its next real order (subject to the strategy's own trading hours, open-position and safety rules).

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

**MONEY NEXT** means F2 (and F3, if set) is armed: the next F1 transit would be a real trade under that pipeline.

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
| `MONEY NEXT` | F2 (and F3) armed: the next F1 transit would be a trade under this pipeline |
| `ALARMING NOW 10:42:13` | The deepest string just ended with the alert pattern (default `1000`); one sound was played |
| `last alert 10:42:13` | When the alert last fired; stays so you can read it later |

In the viewer, 1s are green and grouped by 10 counted from the newest bit, an alarming setup gets an orange frame, and a section header turns yellow if its file has not been updated for 3 minutes (slow market, or NinjaTrader stopped).

The chart HUD shows the same items on one short line per setup (last 30 bits).

## Trading workflow: Layer 1, 2 and 3

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
   - **Trade-outcome exit pattern** on today's real results, e.g. `111` = stop after three real wins in a row (lock in a good run).
   - **Max real losses in a row**, e.g. 3 = stop after three straight real losses (cap the bad run).
   - **Trading hours / strategy life** so it never runs into hours you did not choose.
5. **Review afterwards** from the strategy's log: trades, win rate against the base rate, and which exit rule ended it.

What this does and does not do: it removes in-the-moment hesitation and caps the damage of a bad stretch, but it does not create an edge. Any edge still has to come from your choice of when to run it, so keep score honestly. Also check the platform's connection-loss setting and glance at the strategy now and then: a long disconnect can still leave a position unprotected.

## Risk and discipline

Trade flat size, put the stop in with every entry, and judge yourself against the base win rate over at least a few hundred trades.

| Geometry | Break-even win rate (before costs) | Notes |
| --- | --- | --- |
| Stop 1 brick, target 2 bricks | 33.3% | The next brick decides the trade |
| Stop 1 brick, target 3 bricks | 25.0% | Needs two bricks your way before one against |

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
3. Check each section header: instrument, brick size, last brick time, and the back fill note.
4. Check the NinjaScript Output window or `<LogBaseName>_setup_check.txt` for any setup turned OFF by a typo.

**Default setups** (all with F2 = `01,10`, F3 blank, alert `1000`)

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
| Log Folder / Log Base Name | `C:\temp` / `Renko_MultiRecorder` | Use a different base name on each chart (e.g. `MNQ40_Recorder`, `MNQ80_Recorder`) |
| Alert sound file | `Alert2.wav` | In NinjaTrader's sounds folder; blank = silent |
| Alert hours (New York time, ET) | 9:30–16:00 ET (6:30 AM–1:00 PM PT) | No sound outside; the text still updates |

**Files in the log folder**

- `<LogBaseName>_S01_MERGED_F1-10_F2-01_10_F3-off_<date-time>.csv` — one per setup per enable: every outcome with full strings (back-fill rows marked `replay=1`), resets and alerts noted.
- `<LogBaseName>_state.json` — the live state, rewritten after every brick; read by the viewer.
- `<LogBaseName>_setup_check.txt` — how each slot was understood.

**Troubleshooting**

| Symptom | Check |
| --- | --- |
| Viewer shows nothing | Same folder as the recorder's Log Folder; "Enable logs" ON; Chrome or Edge, not Firefox |
| Viewer jumps between two charts' data | Two recorders share a Log Base Name: give each its own |
| Header yellow, "min ago" | NinjaTrader stopped or the market is slow; an old recorder's file can be deleted |
| A setup shows OFF | A typo in a pattern: only `0 1 * ?` and commas are allowed |
| No alert sound | Outside alert hours, sound file blank or missing, or Windows volume |
| New defaults not showing | Remove and re-add the indicator; re-save any old template |
