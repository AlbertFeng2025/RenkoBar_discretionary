# Renko Continuation Guide — by Current Code
*Continuation trading with the existing Renko **Transit** strategy code (no new code)*
**Instrument in the examples:** MNQ (Micro Nasdaq, $2 per point) · **Chart:** Renko 40 ticks (1 brick = 10 points)
**Code:** `Scalper_Renko_TransitLONGrepeat_Layer2/3.cs`, `Scalper_Renko_TransitSHORTrepeat_Layer2/3.cs`, `Renko_mergedTransit_Layer2/layer3.cs`
**Date:** 2026-11-01

---

> ## ⚠️ WARNING — advanced users only
> This is **not** what the code was originally built for. The strategies were built for the **transit**
> (F1 ends in **0**, stop 1 brick, target 2 bricks). Turning them into a **continuation** strategy means
> changing several settings **together** — the F1 pattern, the stop, the target, the qty rule and the
> exit pattern. If any one of them does not match the others, the strategy still runs and still
> trades, but it no longer does what you think, and the logs and the viewer stop agreeing with your
> real account.
>
> * Use it only if you fully understand every section of this guide.
> * **Test in Sim101 first**, on a normal day **and** a fast day, before any real money.
> * Nothing here is an edge. In theory a random market gives a continuation setup **zero** profit
>   before costs (section 8). The settings decide *how* you win and lose, not *whether* you win.
> * A rising qty rule with this bracket can lose a lot of money in a short losing run (section 6).
> * If something in a log or on the viewer looks wrong, **turn real orders off** first, then find out why.

> ## ⚠️ SECOND WARNING — one mode at a time
> **Pick transit OR continuation before the session and keep it all the way.** Do not chase transit when
> the market "swings" and switch to continuation when it "looks trending". This is not about how smart
> anyone is — switching is a job where everyone makes mistakes under pressure, and it does not pay.
>
> **Why:**
> 1. **Too many settings must change together** — the F1 ending (0 ↔ 1), the stop and target
>    (10/20 ↔ 20/10 on MNQ 40-tick), the qty rule and the exit pattern. Miss one and the strategy still
>    trades, but no longer does what you think. That is easy to miss in a fast market — exactly when the
>    urge to switch is strongest.
> 2. **The numbers you read flip their meaning.** "Normal" is about **33 %** 1's for transit and about
>    **67 %** for continuation. A run of 0's is common in one mode and rare in the other, so habits,
>    instincts and alert patterns tuned for one mode mislead you in the other.
> 3. **"It looks trending" is usually seen too late.** The research found the strings close to random,
>    so a trend is mostly visible after the fact. Switching on a feeling tends to catch the end of a
>    move and adds the selection bias the research warned about.
> 4. **No edge to gain.** In theory both modes break even before costs; switching adds mistakes without
>    adding expected profit.
>
> **How to do it in practice:**
> * **One mode per strategy instance**, decided **before** the session, never edited during it.
> * **Save each mode as a NinjaTrader strategy template** with a clear name, e.g. "LONG TRANSIT 40" and
>   "LONG CONT 40", so the settings always load as a matching set.
> * **Put the mode in the Log Base Name**, e.g. `MNQ40-CONT-11-01-2026-8AM`, so the viewer and the logs
>   show which mode you are looking at.
> * If you want both, run them as **two separate, pre-configured strategies** — never by editing one live.

---

## 1. Transit vs. continuation

| | **Transit** (original) | **Continuation** (this guide) |
|---|---|---|
| F1 pattern ends in | **0** — e.g. `10`, `100`, `1000` | **1** — e.g. `01`, `001`, `0001` |
| Meaning (LONG book) | last brick went **against** you (red) → bet the next one **reverses** to green | last brick went **with** you (green) → bet the next one **continues** green |
| Next brick needed to win | a **reversal** brick = price must move **2 bricks** | a **same-color** brick = price must move **1 brick** |
| Stop / target that fit | stop **1 brick**, target **2 bricks** | stop **2 bricks**, target **1 brick** |
| MNQ 40-tick settings | Stop **10** pt, Target **20** pt | Stop **20** pt, Target **10** pt |
| Random-market win rate | **33.3 %** (1 in 3) | **66.7 %** (2 in 3) |
| Win / loss size | +20 / −10 | +10 / −20 |
| Break-even win rate | 33.3 % | **66.7 %** |

The code is the same — only the settings change. "LONG" still opens a LONG position, "SHORT" still opens
a SHORT position. In the **SHORT** book everything is mirrored (1 = red brick = good for the short).
In the **merged** book, F1 = `01` fires in both directions: red→green → LONG continuation,
green→red → SHORT continuation.

---

## 2. Why the stop and target must be 2 bricks / 1 brick

In NinjaTrader's Renko, from the close of a brick:
* a brick of the **same color** forms when price moves **1 brick** further (MNQ 40-tick: **10 pt**);
* a brick of the **opposite color** (reversal) forms only after price moves **2 bricks** back (**20 pt**).

LONG continuation, entry at the close **P** of the green brick:

| Next brick | It closes at | With **stop 20 / target 10** |
|---|---|---|
| green (continuation) | **P + 10** | exactly the **target** → real **win** |
| red (reversal) | **P − 20** | exactly the **stop** → real **loss** |

So every trade is decided by the **very next brick**, and the "next brick" outcome the strategy uses for
its filters (F1/F2/F3 strings) is **the same event** as your real win/loss. That is the only setup where
the filter strings and your real money agree.

### What goes wrong with other numbers (MNQ 40-tick, F1 ends in 1)

| Stop / Target | What happens | Filters agree with real money? |
|---|---|---|
| **20 / 10** ✅ | decided by the next brick, as above | **Yes** |
| 10 / 10 | the stop (P−10) is hit *before* a red brick can form; price may then turn up and print a green brick → filter says **win**, you **lost** | No |
| 20 / 20 | a green brick closes at P+10, target not reached → filter says **win**, trade still open, may still lose | No |
| 10 / 20 (transit bracket) | wrong for continuation in both directions | No |
| 30 / 10, 40 / 10 … | bigger stop than the reversal brick → trade can stay open over many bricks; new signals are skipped (busy) | No |

**Rule:** for continuation, **Stop = 2 × brick, Target = 1 × brick** — on MNQ 40-tick: **Stop 20, Target 10**.
On another brick size, scale it (80-tick = 20 pt brick → Stop 40, Target 20). The settings are in
**points**, not ticks (MNQ: 4 ticks = 1 point).

---

## 3. Settings checklist (LONG example)

| Setting | Value | Note |
|---|---|---|
| File | `Scalper_Renko_TransitLONGrepeat_Layer3` with **F3 blank** (= Layer 2), or the Layer 2 file | merged Layer 3 also treats F3 blank as Layer 2 (version of 2026-10-28) |
| Enable Real Order | **off** for the first runs | watch the logs first |
| F1 Pattern | `01` (or `001`, `0001` …) | **must end in 1** |
| F2 Pattern | `01,10` | filter on the F1 outcome string |
| F3 Pattern | blank | Layer 2 behaviour |
| Stop loss (points) | **20** | 2 bricks |
| Profit target (points) | **10** | 1 brick |
| Use market entry | on | entry at the brick close; the bracket is anchored to the brick close |
| Base quantity | 1 to start | |
| Qty rule | see section 6 — **re-think it**, do not keep the transit rule by habit | |
| Max Real Loss In A Row (breaker) | small (e.g. 3–4) | |
| Trade-outcome exit pattern | see section 6 | |
| Trading hours | your choice | |
| Log Base Name | new unique name | |

To **watch** the same setup on the recorder indicator / browser viewer, add a setup slot:
Book **LONG**, F1 `01`, F2 `01,10`, F3 blank — on the **same chart**.

---

## 4. The four strings — and which rule reads which

| String | Built from | 1 / 0 means | Read by |
|---|---|---|---|
| **rawString** | every brick | LONG book: 1 = green, 0 = red (SHORT book: 1 = red) | the **F1** pattern |
| **F1 outcome** (`filter1Outcome`) | the next brick after **every** F1 signal | 1 = that brick went your way (would-be win) | the **F2** pattern |
| **Filtered (theory) string** — F2 outcome (`filter2Outcome`); in the viewer the last line, "F2 = money" | the next brick after every signal **taken while F2 was armed** | would-be win / loss of the trades the filter allows | the **F3** pattern (if used) |
| **Real money string** (`realTradeOutcome`; today's part = `sessionRealOutcome`) | **actual filled trades** | 1 = more contracts filled at the target than at the stop | the **qty rule**, the **exit pattern** (today's part) and the **breaker** (losses in a row) |

* **The filter (F1, F2, F3) decides WHEN to trade** — it is theoretical, made of would-be trades.
* **The qty rule, exit pattern and breaker decide HOW MUCH and WHEN TO STOP** — they read only real trades.
* Convention everywhere: **1 = win (money gained), 0 = loss.**

### Which strings each strategy log shows

The qty rule, exit pattern and breaker use **only the real money string** in all three strategies —
even where the log also prints the theory strings. The theory strings in a log are there for you to
read (and, when F3 is set, to arm F3).

| String | Merged log | LONG / SHORT log |
|---|---|---|
| F1 outcome (theory, Layer 1) | yes — `f1out=` | yes — column `filter1Outcome` |
| Last-layer outcome (theory, F2) | **yes** — `f2out=` | **only Layer 3 with F3 set** (`filter2Outcome`); Layer 2, or Layer 3 with F3 blank: **no** (column missing or empty) |
| Real money trades — **used by the qty rule, exit pattern, breaker** | yes — `real=` on each `TRADE CLOSED`, `[QTY] sessionReal=` | yes — column `realTradeOutcome` |

So to check your qty rule or exit, always look at the real money string: the `realTradeOutcome`
column in LONG/SHORT, the `real=` lines in merged.

---

## 5. Worked example — MNQ 40-tick, LONG, F1 = `01`, F2 = `01,10`

**Settings:** stop 20, target 10, base qty 1, qty rule `("00":2),("000":3),("0000":4)`,
exit pattern `1`, breaker 5, F3 blank.
**Assumption:** every condition is met at every signal — flat, inside trading hours, no gate blocking,
real orders on, and every order fills exactly at its price. (Section 7 shows what changes when not.)

**Brick colors (G = green = 1, R = red = 0):**
```
#   1 2 3 4 5 6 7 8 9 10 11 12 13 14 15 16 17 18 19 20 21 22 23 24 25 26 27 28 29 30 31
    G G R G G R G R G R  G  G  R  G  R  G  R  G  R  G  R  G  G  R  G  R  G  G  R  G  G
```
**rawString:** `1101101010110101010101101011011`

**Signal:** F1 = `01` → a red brick followed by a green brick. The strategy buys at that green brick's
close; the next brick decides the result.
**F2 = `01,10`:** armed when the F1 outcome string ends in `01` or `10` (the last two outcomes differ).

| # | Brick | Close | What happens | F1 outcome | Filtered (theory) | Real money | Qty |
|---|---|---|---|---|---|---|---|
| 1 | G | 25010 | – | – | – | – | |
| 2 | G | 25020 | – | – | – | – | |
| 3 | R | 25000 | – | – | – | – | |
| 4 | G | 25020 | signal (R,G), F2 not armed → watch only | – | – | – | |
| 5 | G | 25030 | would-be **win**; F2 not armed | `1` | – | – | |
| 6 | R | 25010 | – | `1` | – | – | |
| 7 | G | 25030 | signal, not armed → watch only | `1` | – | – | |
| 8 | R | 25010 | would-be **loss**; F2 **armed** (`10`) | `10` | – | – | |
| 9 | G | 25030 | signal + armed → **BUY 1** @ 25030, stop 25010, target 25040 | `10` | – | – | **1** |
| 10 | R | 25010 | stop: **−20 pt × 1** (−$40); F2 not armed | `100` | `0` | `0` | |
| 11 | G | 25030 | signal, not armed → watch only | `100` | `0` | `0` | |
| 12 | G | 25040 | would-be **win**; F2 **armed** (`01`) | `1001` | `0` | `0` | |
| 13 | R | 25020 | – | `1001` | `0` | `0` | |
| 14 | G | 25040 | signal + armed → **BUY 1** (real `0`: no rule matches → ×1) | `1001` | `0` | `0` | **1** |
| 15 | R | 25020 | stop: **−20 pt × 1** (−$40); F2 **armed** (`10`) | `10010` | `00` | `00` | |
| 16 | G | 25040 | signal + armed → **BUY 2** (real ends `00` → ×2) | `10010` | `00` | `00` | **2** |
| 17 | R | 25020 | stop: **−20 pt × 2** (−$80); F2 not armed | `100100` | `000` | `000` | |
| 18 | G | 25040 | signal, not armed → watch only | `100100` | `000` | `000` | |
| 19 | R | 25020 | would-be **loss**; not armed | `1001000` | `000` | `000` | |
| 20 | G | 25040 | signal, not armed → watch only | `1001000` | `000` | `000` | |
| 21 | R | 25020 | would-be **loss**; not armed | `10010000` | `000` | `000` | |
| 22 | G | 25040 | signal, not armed → watch only | `10010000` | `000` | `000` | |
| 23 | G | 25050 | would-be **win**; F2 **armed** (`01`) | `100100001` | `000` | `000` | |
| 24 | R | 25030 | – | `100100001` | `000` | `000` | |
| 25 | G | 25050 | signal + armed → **BUY 3** (real ends `000` → ×3) | `100100001` | `000` | `000` | **3** |
| 26 | R | 25030 | stop: **−20 pt × 3** (−$120); F2 **armed** (`10`) | `1001000010` | `0000` | `0000` | |
| 27 | G | 25050 | signal + armed → **BUY 4** (real ends `0000` → ×4) | `1001000010` | `0000` | `0000` | **4** |
| 28 | G | 25060 | target: **+10 pt × 4** (+$80) → **exit `1` matched → strategy shuts down** | `10010000101` | `00001` | `00001` | |
| 29 | R | 25040 | – | `10010000101` | `00001` | `00001` | |
| 30 | G | 25060 | signal + armed, but the strategy is stopped → **no order** | `10010000101` | `00001` | `00001` | |
| 31 | G | 25070 | would-be **win** (theory only) | `100100001011` | `000011` | `00001` | |

### What the example shows
1. **Each trade is decided by the next brick** — the target sits exactly where a green continuation
   brick closes, the stop exactly where a red reversal brick closes.
2. **Filtered (theory) = real money** (`00001`) while every condition is met. After the shutdown
   (bricks 30–31) the theory string keeps growing (`000011`) but the real string does not — the viewer
   would show the extra `1`, your account would not.
3. **The F1 outcome string had wins (bricks 12, 23) that were not traded** — F2 was not armed then.
   The real string still reads `0000`: **the qty rule and the breaker only see real trades**, so the
   quantity kept climbing 1 → 1 → 2 → 3 → 4 across those untraded wins.
4. A single `0` matches no rule (the table starts at `00`), so trade 2 was still ×1.
5. **Exit `1`** ended the session at the first real win.
6. **Breaker:** with "Max Real Loss In A Row" = **4** instead of 5, the strategy would have shut down at
   brick 27, *before* the ×4 trade.
7. **Money:** losses 20 × (1+1+2+3) = **140 pt**, win 10 × 4 = **40 pt** → **−100 pt (−$200)** — even
   though the session ended on a win. With target 10 / stop 20 one win cannot repay several losses at
   these multipliers (section 6).

---

## 6. Qty rule and exit pattern for continuation

### How they work (same convention as transit)
* Both read the **real money string of today** (resets at the new session). 1 = win, 0 = loss.
* **Qty rule:** the **longest** pattern that matches the end of the real string sets the multiplier;
  qty = base quantity × multiplier. No match → ×1.
* **Exit pattern:** when the end of the real string matches it, the strategy closes everything and stops.
  (LONG/SHORT read today's real trades; the **merged** strategy reads all real trades since it was
  enabled — not cleared at the new session. Both are real money trades; only the time window differs.
  With exit `1` this makes no difference — see section 10 for an example where it does.)
* **Breaker:** counts **real** losses in a row; at the limit the strategy stops. Resets each new day.

### Format — type it directly in NinjaTrader
`("00":2),("000":3),("0000":4)`
* straight double quotes `"` — **not** curly `“ ”` (they appear when copying from Word, e-mail or a phone);
* pattern = only `0` and `1`, no spaces inside it; spaces around brackets, commas and the colon are fine;
* whole numbers only (`2.5` is read as `2`);
* `0` as a multiplier = skip the trade — **trap:** a skipped trade adds nothing to the real string, so it
  stays skipped for the rest of the session (it is effectively a session stop);
* LONG/SHORT refuse a multiplier above **20**; the **merged** strategy has **no limit** — a typo like `40`
  really trades 40×;
* after enabling, check the log: LONG/SHORT `[QTY RULE] ACTIVE: 00->x2 000->x3 0000->x4`,
  merged `[QTY RULE] parsed 3 rows`.

### Why the transit qty rule does not fit continuation
Each loss (−20) is twice a win (+10). To end **positive** with one win after a losing run, the next
quantity must be **more than twice everything already lost**:

| Real losses so far (qty used) | Points lost | Next qty needed to finish positive |
|---|---|---|
| 1 (1) | 20 | **3** |
| 2 (1, 3) | 80 | **9** |
| 3 (1, 3, 9) | 260 | **27** — above the LONG/SHORT limit of 20 |

That grows very fast. A gentle rule like the example (×2, ×3, ×4) **reduces** the damage of a run but
does **not** win it back. A steep rule wins it back only while the run is short — and one long run can
cost more than many good days. In a random market neither has an edge (section 8).

### Exit pattern ideas for continuation
| Exit pattern | Effect with a ~67 % real win rate (theory) |
|---|---|
| `1` | stops after the first real win — usually after **one** trade |
| `11` | stops after two real wins in a row |
| `01` | stops at the first win **after** a loss (the "recovered" moment) |
| blank | no exit pattern — only the breaker, hours and margin cutoff stop it |

### Losing runs (random theory, loss = 1/3 per trade)
| Real losses in a row | Chance at any given point |
|---|---|
| 2 | 1/9 ≈ **11 %** |
| 3 | 1/27 ≈ **3.7 %** |
| 4 | 1/81 ≈ **1.2 %** |
| 5 | 1/243 ≈ **0.4 %** |

Rare per point, but with many trades a day they **will** happen. Set the breaker to the largest loss
you accept.

---

## 7. When the theory string and the real money string differ

The viewer's last line ("F2 = money") and the strategy's CSV column `filter2Outcome` are **theory**:
every signal the filter allowed, scored by the next brick. The real money string is what filled.
They differ when:

1. **A signal was not traded** (the theory string gets an extra bit): position still open (busy),
   outside trading hours, margin cutoff, trend/speed gate, qty rule returned 0, strategy already shut
   down (exit pattern / breaker), real orders off. Example: if brick 16 had been outside trading hours,
   the theory string would still be `000…` but the real string would be one `0` shorter — and the next
   trade's qty would be ×1 instead of ×3.
2. **Fills:** the target is touched but not filled and the trade later loses (theory **1**, real **0**);
   with several contracts, more of them stopped out than filled at the target (majority rule → real
   **0**). Stop slippage costs money but does not change the bit.
3. **Stop / target do not match the brick geometry** (section 2).
4. **Different start:** the viewer includes history from before the strategy was enabled; the real
   string starts at enable (today's part resets each new day). Compare only the newest bits.

When they differ, **the real money string is the one that drives your size and your exit.**
The strategy log tells you why: `[OUTSIDE HOURS]`, `skipped (busy)`, `[QTY SKIP]`, and `winQty` /
`lossQty` on each `TRADE CLOSED` line.

---

## 8. The theory — what to expect from a random market

* From any brick close, the next brick is the **same color 2/3** of the time and **reverses 1/3**
  (a reversal needs twice the distance).
* Only the **last** brick matters: `01`, `001`, `0001` all give the same 2/3 chance; longer patterns
  give **fewer** trades, not better ones.
* Continuation, stop 20 / target 10: (2/3 × 10) − (1/3 × 20) = **0 points per trade** before
  commission and slippage. Costs make it slightly negative.
* So the 1's rate of the F1, filtered and real strings should hover around **66.7 %**. A real edge would
  be a rate that stays **clearly above 66.7 %** over hundreds of trades. Short stretches above it are
  normal luck.
* Filter patterns built from 0's mean something much rarer here than in transit trading: e.g. a
  Layer 3 F3 = `1000` (three losses after a win) happens only about (1/3)³ ≈ 4 % of the time after a win.

---

## 9. Before going live — checklist

- [ ] F1 ends in **1**; Stop = **2 bricks**, Target = **1 brick** (MNQ 40-tick: 20 / 10).
- [ ] Qty rule re-thought for −20 / +10; typed in NinjaTrader with straight quotes; `[QTY RULE]` log line checked.
- [ ] Exit pattern and breaker chosen on purpose; breaker small.
- [ ] Real orders **off** first; the log shows signals, `[F2 MATCH]` / `[F2 NO MATCH]`, and would-be results as expected.
- [ ] Sim101 run on a normal and a fast day; every `TRADE CLOSED` line checked (winQty / lossQty, slippage).
- [ ] The recorder / viewer watches the same setup on the **same chart**; new unique Log Base Name; the LIVE header shows that name.
- [ ] You can explain every row of the table in section 5 without looking. If not, re-read before trading real money.

---

## 10. Same idea in the SHORT and merged strategies

Everything above also works in the **SHORT** file and the **merged** file. The rules (F1 must end in **1**,
Stop = 2 bricks, Target = 1 brick, 1 = win / 0 = loss, the filter decides *when*, the **real money
string** drives the **qty rule, exit pattern and breaker**) are the same. The main difference is
**which position the strategy opens**:

| File | F1 = `01` means | Opens |
|---|---|---|
| `Scalper_Renko_TransitLONGrepeat_Layer2/3` | red → green, bet on another **green** | **LONG** only |
| `Scalper_Renko_TransitSHORTrepeat_Layer2/3` | green → red, bet on another **red** (in this book 1 = red) | **SHORT** only |
| `Renko_mergedTransit_Layer2/layer3` | red → green **or** green → red | **LONG or SHORT** — the direction of whichever pattern matched |

For SHORT the bracket is mirrored: stop **above** the entry (+20 pt), target **below** (−10 pt).

### Merged — what is different
* **One F1 outcome string for both directions.** LONG and SHORT signals are scored in the order they
  happen, all in one string (1 = that trade's direction won). F2 (and F3) filter that mixed string.
* **One real money string for both directions.** Every real LONG and SHORT trade goes into the same
  string, so the **qty rule, exit pattern and breaker count LONG and SHORT results together** — a
  LONG loss followed by a SHORT loss is `00`.
* **One position at a time.** A signal that comes while a trade is still open is skipped (busy).
* **No qty limit** in the merged file (LONG/SHORT refuse above ×20) — check the `[QTY RULE] parsed …`
  log line every time.
* **Exit pattern — same real money trades, different time window.** All files read **real** trades
  (never the theory string); they differ only in how far back they look:

  | File | Exit pattern reads | Reset |
  |---|---|---|
  | LONG / SHORT | real trades of **today** (`sessionRealOutcome`) | at the new trading day |
  | Merged | real trades **since the strategy was enabled** (`realTradeOutcome`) | only when you re-enable it |

  It matters only if the strategy runs **across the session change** (left on overnight) **and** the exit
  pattern is longer than one bit. Example with exit `11`: yesterday's last real trade won and today's
  first real trade wins → **merged** sees `…11` and shuts down; **LONG/SHORT** see only today's `1` and
  keep trading. With exit `1`, or when you enable the strategy fresh each day, they behave the same.
  The **qty rule** reads today's real trades in all three files.
* **F3 blank = Layer 2** in the merged Layer 3 (version of 2026-10-28), and the merged file fills the
  filtered (F2 outcome) string either way, so it can be compared with the real string directly.

**Tiny merged example** (F1 = `01`, every signal traded, MNQ 40-tick, stop 20 / target 10):

| # | Brick | Signal | Next brick → result | F1 outcome = real (if all traded) |
|---|---|---|---|---|
| 1–2 | G G | – | | |
| 3 | R | green → red → **SELL** (SHORT continuation) | brick 4 red → **win +10** | `1` |
| 4 | R | – | | |
| 5 | G | red → green → **BUY** (LONG continuation) | brick 6 green → **win +10** | `11` |
| 6 | G | – | | |
| 7 | R | green → red → **SELL** | brick 8 green → **loss −20** | `110` |
| 8 | G | red → green → **BUY** — the short's stop was hit as this brick formed, so the strategy is normally flat again (if the stop's fill report arrives a moment late, this signal is skipped as busy) | brick 9 decides | |

The string `110` mixes SHORT, LONG and SHORT results. A qty rule or exit pattern does not know or care
which direction each bit came from — only win or loss.
