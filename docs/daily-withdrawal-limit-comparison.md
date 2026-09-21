# Daily withdrawal limit — AI maintainability comparison

Same product change, applied **one app at a time** as a measured experiment:

1. `apps/1-legacy` — controllers → `IPaymentService` → MediatR → repositories
2. `apps/2-balanced` — minimal APIs → feature handlers → account/transaction modules
3. `apps/3-ai-native` — one file per operation, closed result unions, `AGENTS.md` / `FEATURE-MAP.md`

Behavior is defined in [`SPEC.md`](SPEC.md). Customers can `GET`/`PUT` `/customers/{customerId}/daily-withdrawal-limit`. Withdrawals that would exceed today’s UTC total (when a limit is set) return `DailyLimitExceeded` and do not call the provider.

All three test suites passed after the change:

| App | Tests |
|-----|-------|
| Legacy | 18 passed |
| Balanced | 12 passed |
| AI-native | 12 passed |

---

## How tokens were counted

These are **source-token estimates** from a single Cursor agent run, not billed prompt/completion tokens. The heuristic is the common GPT-family rule **1 token ≈ 4 characters**.

| Bucket | What was counted |
|--------|------------------|
| **Reading** | Files actually opened to understand that app, measured **before** crediting later edits in those same files (post-edit size minus the feature delta). |
| **Implementing** | Tokens in **new files** plus the **added/changed portions** of modified files (not the whole file). |
| **Not counted per app** | Shared repo docs (`README.md`, `SPEC.md`, `docs/ai-native-csharp-principles.md`, ~4,083 tokens) loaded once up front. Tool-call JSON, compiler output, and chat overhead. |

These figures are comparable **across the three apps in that experiment**. They are not an official tokenizer dump (cl100k/o200k/tiktoken was not available in the environment).

A sequential confounder: the SQLite `DateTimeOffset` query limitation was discovered while testing **balanced**, then reused in the other two. Balanced paid the debug cost; the others did not rediscover it.

---

## Headline numbers

| Metric | 1. Legacy | 2. Balanced | 3. AI-native |
|--------|----------------:|------------:|-------------:|
| Files opened to understand the app | 32 | 18 | 12 |
| Estimated **reading** tokens | **6,509** | **4,270** | **6,359** |
| Files created | 11 | 4 | 2 |
| Files modified | 9 | 7 | 7 |
| **Files touched** | **20** | **11** | **9** |
| Estimated **implementation** tokens | **3,878** | **1,919** | **2,522** |
| **Reading + implementation** | **10,387** | **6,189** | **8,881** |
| Relative total (legacy = 1.00) | 1.00 | 0.60 | 0.86 |

Lowest total tokens: **balanced**. Fewest files touched: **AI-native**. Highest cost on both axes: **legacy**.

```
Reading tokens
  Legacy    ████████████████████████████████  6,509
  Balanced  █████████████████████             4,270
  AI-native ███████████████████████████████   6,359

Implementation tokens
  Legacy    ████████████████████████████████  3,878
  Balanced  ████████████████                  1,919
  AI-native █████████████████████             2,522

Files touched
  Legacy    ████████████████████  20
  Balanced  ███████████           11
  AI-native █████████              9
```

---

## What had to be understood (reading)

### 1. Legacy — 32 files, ~6,509 tokens

The withdrawal path is a deep hop chain. Adding a customer-owned setting plus a ledger check required opening:

- HTTP: `PaymentsController`, `CustomersController`, contracts, exception middleware
- Application façade: `IPaymentService`, `PaymentService`
- MediatR: `WithdrawCommand` / handler, `GetBalanceQuery` / handler (as the pattern for a new query)
- Domain: `Customer`, `PaymentTransaction`, `TransactionType`, `IUnitOfWork`, both repository interfaces, exception types
- Infrastructure: `AppDbContext`, both repositories, seeder
- Tests: handler tests with Moq

Several of those files exist only as **mirrors** of one another (`WithdrawalRequest` vs `WithdrawalRequestDto`, `BalanceResponse` vs `BalanceResultDto`). Each extra hop is a file the agent must confirm before editing.

### 2. Balanced — 18 files, ~4,270 tokens

The same questions map to fewer, more vertical files:

- `WithdrawHandler` + `PaymentRequest` / `PaymentResponse`
- `AccountModule` / `TransactionModule`
- `Customer`, `OperationStatus`, `AppDbContext`
- `GetBalanceHandler` as the GET pattern
- `Program.cs` for DI + routes
- One HTTP test file

No separate Domain/Application/Infrastructure projects. Feature handlers already sit next to the modules they use. This was the **cheapest app to read** in token terms.

### 3. AI-native — 12 files, ~6,359 tokens

`README.md` says: read `AGENTS.md` and `FEATURE-MAP.md` first. That worked. The playbook names the exact files (`Withdraw.cs`, `_Template.cs`, `Program.cs`, `FEATURE-MAP.md`, tests). No repository interfaces or MediatR types to discover.

Reading **tokens** were still close to legacy because each operation file is intentionally dense: header JSON examples, closed `Outcome` unions, HTTP mapping, and EF in one place. `Withdraw.cs` alone is ~1,400 tokens after the change. The agent opened fewer files, but each file was larger.

That is the intended tradeoff: **local reasoning** instead of a shallow graph of tiny types.

---

## What had to be written (implementation)

Same behavior in every app:

- `Customers.DailyWithdrawalLimit` (`decimal?`, null = unlimited)
- `GET` + `PUT` `/customers/{id}/daily-withdrawal-limit`
- Withdrawal check after insufficient-funds, before the dummy provider
- `DailyLimitExceeded` outcome
- Tests for set/change, within-limit withdraw, over-limit withdraw

### 1. Legacy — 11 new + 9 modified, ~3,878 tokens

Layering forced a full vertical slice of **types that do not exist in the other apps**:

| Layer | New types |
|-------|-----------|
| Domain | `DailyLimitExceededException`; repository method on `IPaymentTransactionRepository` |
| Application | command + handler, query + handler, two DTOs, `IPaymentService` / `PaymentService` methods |
| API | two contracts; two controller actions |
| Tests | two new handler test classes + extra withdraw cases |

Nothing in this style lets you add an operation by copying one file. MediatR registration is convention-based (no extra DI line), but every other layer still needs a file.

### 2. Balanced — 4 new + 7 modified, ~1,919 tokens

Handlers already aggregate modules, so the new operations are two small handler classes plus request/response records. The limit check is a method on `TransactionModule`; the write is a method on `AccountModule`. DI is two `AddScoped` lines in `Program.cs`.

This produced the **smallest implementation** because there is no DTO/command/query/contract duplication, and tests were appended to the existing HTTP spec file.

### 3. AI-native — 2 new + 7 modified, ~2,522 tokens

Followed `AGENTS.md` literally:

1. Copy `_Template.cs` → `GetDailyWithdrawalLimit.cs` and `SetDailyWithdrawalLimit.cs`
2. Keep request, response, outcome, EF, and HTTP mapping in those files
3. Map routes in `Program.cs` with `new AppDbContext(dbOptions)` (no feature DI)
4. Update `FEATURE-MAP.md` and `AGENTS.md`
5. Add `When_*` tests
6. Edit `Withdraw.cs` in place for the enforcement rule

Implementation tokens sit between the other two: two verbose self-contained feature files (comments included) rather than eleven thin types. **Files touched is the win**, not raw token count of the new files.

---

## Qualitative comparison

| Question | Legacy | Balanced | AI-native |
|----------|--------|----------|-----------|
| Where does withdrawal live? | Handler + service + two repos + factory | `WithdrawHandler` + two modules | `Features/Withdraw.cs` |
| How do I add GET/PUT limit? | New command, query, DTOs, contracts, interface methods | New feature folder + two handlers | Copy `_Template.cs` twice |
| How do I find the files? | Search the four projects | Search `Features/` and `Modules/` | `FEATURE-MAP.md` |
| Business errors | Exceptions + middleware status codes | `OperationStatus` on HTTP 200 | Same status union, local to the file |
| Test style | Moq unit tests per handler | HTTP `WebApplicationFactory` | HTTP `WebApplicationFactory` |
| Hidden coupling | High (UoW, factory, MediatR, mapping) | Medium (DI of handlers/modules) | Low (db options constructed at the route) |

### Why AI-native used more tokens than balanced

The extra tokens are mostly **deliberate verbosity**, not a sign that balanced is more AI-maintainable. AI-native is optimizing a different cost: **cold-start correctness and local completeness**, not **minimum characters in context**.

Balanced shares structure. A GET handler is a short method that calls `AccountModule` and returns a shared response record. AI-native **forbids that sharing**. `AGENTS.md` says to copy validation, keep request/response/`Outcome`/HTTP mapping/EF in one file, and put JSON examples in the header. So `GetDailyWithdrawalLimit.cs` is roughly 60 lines of boilerplate plus comments for the same ~10 lines of business logic.

That shows up twice:

- **Reading:** `Withdraw.cs` is a full operation (comments, union, HTTP, EF). Opening it is expensive. Opening balanced’s `WithdrawHandler` is cheaper, then the agent reuses modules already in context.
- **Implementing:** copying `_Template.cs` emits a whole closed union and `Handle`/`Execute` pair per operation. Balanced adds a small handler and reuses `Money`, `OperationStatus`, and modules.

So “fewer files” did happen (9 vs 11). “Fewer tokens” did not, because each AI-native file is written to be self-sufficient.

The weak assumption is: **local reasoning ≈ fewer tokens**. What the principles actually buy is **fewer hops and less irrelevant context**:

- You should not need `IPaymentService` → MediatR → `IUnitOfWork` to answer “what happens on withdraw?”
- You *should* pay to have statuses, mapping, and persistence in the file you are editing.

Token count treats a useful comment, a duplicated `Outcome` union, and a stray `IUnitOfWork` the same. They are not the same. Legacy’s extra tokens are **fan-out** (types that exist only to satisfy layers). AI-native’s extra tokens are **in-file documentation and duplication**. Balanced sits in the middle: shared modules keep the delta small **once the agent already understands the pattern**.

The measurement also overstated AI-native reading a bit. Deposit was opened as a “sibling” even though `FEATURE-MAP.md` already pointed at `Withdraw.cs`.

| Choice | Token effect | Correctness effect |
|--------|----------------|--------------------|
| Closed `Outcome` union + exhaustive `switch` | more generated code | missing a status is a compile break |
| Header statuses + JSON examples | more reading | the agent sees every outcome next to the code |
| `FEATURE-MAP.md` + `_Template.cs` | extra docs to read | less “where do I put this?” search |
| Copy instead of shared helpers | duplicated lines | changing withdraw decline cannot silently break deposit |
| No feature DI | slightly more `Program.cs` | no forgotten `AddScoped` |

That experiment does **not** prove a higher defect rate or a higher correctness rate. All three apps ended green. A fair correctness comparison would be independent agents, same prompt, counting wrong statuses / missed DI / edited the wrong layer.

If the goal is “cheapest tokens for an agent that already knows the codebase,” balanced is the better design. If the goal is “an agent can change one operation from the map without loading the architecture,” AI-native spends tokens **inside** that operation on purpose.

### Why legacy lost on both

Every new field or outcome fans out across Domain → Application → API. The agent cannot locally reason about “set limit” without also knowing MediatR `IRequest`, the service façade, and two contract types. That is exactly the hop chain [`ai-native-csharp-principles.md`](ai-native-csharp-principles.md) warns against.

### Runtime surprise (SQLite `DateTimeOffset`)

A LINQ `CreatedAt >= startOfUtcDay` filter does not translate on EF Core SQLite. Balanced’s HTTP tests caught a 500; the query was rewritten to load matching rows and filter the UTC day in memory. Legacy unit tests mock the repository, so they would **not** have caught this. AI-native reused the same in-memory filter from the start.

Maintainability is not only “files touched”: **test shape** matters. HTTP tests against SQLite exercised the real query; mocked handler tests did not.

---

## Runtime performance

Same product behavior, measured on one machine after a **Release** build, `ASPNETCORE_ENVIRONMENT=Production`, localhost HTTP/1.1, sequential client, 50 warmup requests then **300 timed samples** per operation. Each app used its own empty SQLite file. Alice was given a 5,000 deposit and a 1,000,000 daily limit before timing so withdrawals stayed on the success path (the extra “sum today’s withdrawals” query still ran).

This is **latency**, not a load test. Absolute numbers are sub-millisecond on loopback; the useful signal is the **difference**.

### Startup and memory

| Metric | 1. Legacy | 2. Balanced | 3. AI-native |
|--------|----------------:|------------:|-------------:|
| Time to first successful `GET /balance` | 2,572 ms | 848 ms | **801 ms** |
| vs legacy | — | **−67%** (3.0× faster) | **−69%** (3.2× faster) |
| vs balanced | +204% | — | **−5.5%** |
| RSS after the run | 152 MB | 141 MB | **141 MB** |
| vs legacy | — | **−7.0%** | **−7.1%** |

Legacy starts slower because it loads four projects, MediatR, a provider factory, and exception middleware. Balanced and AI-native are a single assembly with a shorter DI graph. Working set after the same traffic is within ~11 MB; not a meaningful product difference.

### Request latency (p50 / mean / p95, milliseconds)

| Operation | Legacy p50 | Balanced p50 | AI-native p50 | AI-native vs legacy | AI-native vs balanced | Balanced vs legacy |
|-----------|-----------:|-------------:|--------------:|--------------------:|----------------------:|-------------------:|
| `GET /balance` | 0.590 | 0.564 | **0.495** | **−16%** | **−12%** | −4% |
| `GET .../daily-withdrawal-limit` | 0.584 | 0.555 | **0.486** | **−17%** | **−12%** | −5% |
| `PUT .../daily-withdrawal-limit` | 0.793 | 0.605 | **0.569** | **−28%** | **−6%** | **−24%** |
| `POST /deposits` | 0.789 | 0.794 | **0.761** | −4% | −4% | +1% (noise) |
| `POST /withdrawals` (limit set) | 1.113 | 1.040 | **1.028** | **−8%** | −1% | −7% |

Means and p95 (same samples):

| Operation | Legacy mean / p95 | Balanced mean / p95 | AI-native mean / p95 |
|-----------|------------------:|--------------------:|---------------------:|
| `GET /balance` | 0.625 / 0.727 | 0.590 / 0.651 | **0.519 / 0.568** |
| `GET` limit | 0.609 / 0.716 | 0.564 / 0.617 | **0.514 / 0.547** |
| `PUT` limit | 0.900 / 1.457 | 0.631 / 0.690 | **0.583 / 0.645** |
| Deposit | 0.826 / 0.975 | 0.868 / 0.984 | **0.792 / 0.935** |
| Withdraw + daily-limit check | 1.138 / 1.459 | 1.077 / 1.277 | **1.057 / 1.263** |

Geometric mean of the five p50s (a single “typical request” summary):

| App | Geo-mean p50 | vs legacy | vs balanced |
|-----|-------------:|----------:|------------:|
| Legacy | 0.752 ms | — | +9% slower |
| Balanced | 0.690 ms | **−8%** | — |
| AI-native | **0.640 ms** | **−15%** | **−7%** |

```
Geo-mean p50 (lower is better)
  Legacy    ████████████████████████████████  0.752 ms
  Balanced  █████████████████████████████     0.690 ms  (−8%)
  AI-native ███████████████████████████       0.640 ms  (−15% vs legacy, −7% vs balanced)
```

### How to read the gaps

- **Writes with extra mapping pay the most in legacy.** `PUT` daily-limit p50 is 24% slower than balanced and 28% slower than AI-native: controller → DTO → `IPaymentService` → MediatR command → handler → repository. The other two apps write the column from the handler/feature file.
- **Reads are close.** `GET` balance / GET limit differ by about 0.05–0.10 ms. That is architectural overhead (MediatR + extra allocations), not I/O.
- **The new withdraw check dominates the withdraw endpoint in all three.** p50 jumps from ~0.5 ms (GET) to ~1.0–1.1 ms once today’s withdrawals are loaded and summed in memory. That cost is **shared** (same SQLite limitation, same in-memory filter). AI-native is only ~1% faster than balanced here; the feature, not the layering, is the bottleneck.
- **Deposits are a tie within noise** (legacy vs balanced +0.6% p50). Provider + insert + balance update swamp the hop count.
- **AI-native is the fastest on every timed operation in this run**, including vs balanced. Likely causes: no feature DI (one `AppDbContext` constructed at the route), no module indirection, no MediatR. The margin vs balanced is small except on GETs (~12%).
- These differences would not show up as a user-visible win on a remote network. They *do* show that the AI-native shape did **not** buy maintainability by making the runtime slower. If anything, fewer hops helped.

---

## Conclusions

1. **Files touched per change** (this repo’s stated metric) ranks AI-native (9) < balanced (11) < legacy (20). The layered app needed more than twice as many files as the AI-native app for the same behavior.
2. **Reading tokens** ranked balanced (4.3k) < AI-native (6.4k) ≈ legacy (6.5k). AI-native did not win token-in because operation files are deliberately verbose; it did win **file count** and **searchability** (`FEATURE-MAP.md`).
3. **Implementation tokens** ranked balanced (1.9k) < AI-native (2.5k) < legacy (3.9k). Legacy’s extra tokens are almost entirely duplicated types (DTO/command/query/contract/exception).
4. **The “fewer tokens” assumption is the wrong reading of AI-native.** Local reasoning cuts *irrelevant* hops, not characters in the file you edit. Extra tokens there are documentation, closed unions, and copy-not-share — aimed at cold-start correctness, not a token budget. Balanced is cheaper once the agent already knows the modules.
5. **Runtime ranked AI-native < balanced < legacy.** Geo-mean p50 was 0.640 / 0.690 / 0.752 ms (−15% and −8% vs legacy). Startup was ~3× slower for legacy. The new daily-limit withdraw check (~1.0–1.1 ms) dominates that endpoint in all three; architecture is a few percent on top of SQLite.
6. For an agent working **cold**, AI-native is the only app that documents the exact edit ritual. For an agent that already loaded the composition root, balanced is the cheapest patch. Legacy is expensive to change and slowest to serve.
7. Do not treat mocked-handler coverage as equivalent to in-process HTTP tests when the change touches EF/SQLite.

---

## Appendix — files touched

### Legacy (20)

Created: `DailyLimitExceededException`, `DailyWithdrawalLimitResultDto`, `SetDailyWithdrawalLimitRequestDto`, `SetDailyWithdrawalLimitCommand`, `SetDailyWithdrawalLimitCommandHandler`, `GetDailyWithdrawalLimitQuery`, `GetDailyWithdrawalLimitQueryHandler`, `DailyWithdrawalLimitResponse`, `SetDailyWithdrawalLimitRequest`, `SetDailyWithdrawalLimitCommandHandlerTests`, `GetDailyWithdrawalLimitQueryHandlerTests`.

Modified: `Customer`, `IPaymentTransactionRepository`, `PaymentTransactionRepository`, `AppDbContext`, `WithdrawCommandHandler`, `IPaymentService`, `PaymentService`, `CustomersController`, `WithdrawCommandHandlerTests`.

### Balanced (11)

Created: `SetDailyWithdrawalLimitRequest`, `DailyWithdrawalLimitResponse`, `GetDailyWithdrawalLimitHandler`, `SetDailyWithdrawalLimitHandler`.

Modified: `Customer`, `OperationStatus`, `AccountModule`, `TransactionModule`, `WithdrawHandler`, `Program.cs`, `SpecScenariosTests`.

### AI-native (9)

Created: `GetDailyWithdrawalLimit.cs`, `SetDailyWithdrawalLimit.cs`.

Modified: `Types.cs`, `AppDbContext.cs`, `Withdraw.cs`, `Program.cs`, `FEATURE-MAP.md`, `AGENTS.md`, `SpecScenariosTests.cs`.
