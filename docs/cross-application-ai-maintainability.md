# Cross-application AI maintainability

The earlier comparison in [`daily-withdrawal-limit-comparison.md`](daily-withdrawal-limit-comparison.md) measured **in-process** hop count: controllers → services → MediatR vs one file per operation. That analysis assumed what this repository already is — **one deployable per style**, HTTP in, SQLite out, no other application in the path.

This document is the next ring out: **architecture between applications**. The same product change (daily withdrawal limit, as defined in [`SPEC.md`](SPEC.md)) is used as the running example. None of the three apps were changed for this analysis.

The metric is unchanged from [`ai-native-csharp-principles.md`](ai-native-csharp-principles.md):

> How many files, concepts, and dependencies does an AI agent need to understand before it can safely change this code?

Across process boundaries, add: **how many repositories, contracts, runtimes, and failure modes**.

---

## Why this layer dominates token cost

An in-process MediatR hop is expensive because the agent opens another file. An RPC hop is expensive because the agent must open **another application**:

| In-process hop | Cross-application hop |
|----------------|------------------------|
| Another C# type in the same solution | Another repo, solution, CI, and deploy |
| Compile-time break if a DTO is wrong | Runtime break if a contract is wrong |
| One debugger, one test host | Two processes, two clocks, two logs |
| Mapping is a function | Mapping is a network protocol + versioning |

The principles already warn against `API → Mediator → Handler → Facade → Manager`. The distributed equivalent is:

```
SPA → BFF → gRPC client → proto → Payments RPC
      → gRPC client → proto → Accounts RPC
      → gRPC client → proto → Limits RPC
      → gRPC client → proto → Provider RPC
```

That is the same hop chain with **serialization, partial failure, and ownership split** on every arrow. An agent cannot locally reason about “what happens on withdraw?” by opening `Withdraw.cs`. It has to reconstruct a distributed workflow.

---

## Three topologies for the same withdrawal

All three expose something that looks like `POST /withdrawals` to a client. They differ in **where the use case lives**.

### 1. Legacy — BFF plus chatty RPC

A Backend-for-Frontend owns the HTTP shape the UI wants. The money path lives in several “core” services. The BFF **orchestrates** them.

```
Browser
  │  HTTP POST /withdrawals
  ▼
BFF (web-api)
  │  rpc GetCustomer(customerId)
  │  rpc GetBalance(customerId)
  │  rpc GetDailyLimit(customerId)
  │  rpc SumTodayWithdrawals(customerId)
  │  rpc ChargeProvider(amount, provider)
  │  rpc DebitAccount(customerId, amount)
  │  rpc InsertTransaction(...)
  ▼
Account svc   Ledger svc   Limits svc   Provider svc
```

This is the distributed version of the legacy app: many small types, many hops, each hop “clean.” The ownership model was invented so each team owns a noun (Account, Ledger, Limit). AI pays for that model on **every** product change that crosses nouns.

A slightly milder legacy shape is one RPC instead of seven:

```
Browser → BFF → rpc Withdraw(request) → Payments svc
```

That is still two applications, two contracts, two mappings, two test suites. Better than chatty RPC; still not local.

### 2. Balanced — one capability service, thin channel adapter

HTTP **is** the product API. One payments application owns customer balance, ledger, daily limit, and dummy providers — the same scope as this repo’s apps. A BFF, if it exists, does cookie/session/auth and does **not** re-implement withdraw.

```
Browser
  │  HTTP POST /withdrawals
  ▼
Channel adapter (optional: auth, CSRF, BFF cookie)
  │  HTTP passthrough, same body
  ▼
Payments API (the use case lives here)
  │  in-process modules
  ▼
SQLite (or the database this capability owns)
```

Side effects that are **not** part of the money decision (email, analytics) go out-of-band. They are not on the withdraw return path.

This matches `apps/2-balanced`: handlers aggregate modules **in one process**. The cross-app rule is the same as the in-app rule — process boundary only where a second team, scale, or lifetime actually exists.

### 3. AI-native — end-to-end use case

One application owns the operation from the public HTTP contract to persistence. No BFF translation layer. No RPC for a step the same product change would have to edit anyway.

```
Browser
  │  HTTP POST /withdrawals
  ▼
Payments API
  Features/Withdraw.cs     ← request, rules, EF, Outcome, HTTP mapping
  Data/AppDbContext.cs
```

That is `apps/3-ai-native` taken as a **deployment** choice, not only a file layout. The agent’s work unit is one operation in one repo. `FEATURE-MAP.md` is enough; there is no second map of RPC methods.

“End-to-end” here means **one use case, one runtime**. It does not mean “put the SPA, the bank, and Stripe in the same process.” External systems you do not own stay behind an explicit, boring client. Applications **you** own that always change together should not be networked together.

---

## Worked example: add daily withdrawal limit

Same behavior as the in-app study: persist `Customers.DailyWithdrawalLimit`, `GET`/`PUT` it, reject withdraw with `DailyLimitExceeded` before the provider is called.

| What the agent must find and change | Legacy BFF + chatty RPC | Legacy BFF + one RPC | Balanced capability API | AI-native end-to-end |
|-------------------------------------|-------------------------|----------------------|-------------------------|----------------------|
| Public HTTP contract | BFF routes + BFF DTOs | BFF routes + BFF DTOs | Payments routes | `Withdraw.cs` + two new feature files |
| Internal contract | 2–4 proto/OpenAPI files | 1 proto + generated stubs | none (in-process) | none |
| Limit storage | Limits svc (or Account svc) schema + repo | Payments schema | Payments schema | `AppDbContext` + `Customer` |
| Withdraw enforcement | BFF orchestration **or** Limits+Ledger coordination | Payments handler | `WithdrawHandler` + `TransactionModule` | `Withdraw.cs` |
| New GET/PUT endpoints | BFF + Limits RPC methods | BFF + Payments RPC | two handlers | copy `_Template.cs` twice |
| Error model | gRPC status **and** BFF ProblemDetails **and** UI copy | two mappings | one `OperationStatus` | closed `Outcome` union in the file |
| Tests | BFF tests with mocked RPC + each svc’s unit tests | BFF mock + Payments tests | HTTP tests on one host | HTTP tests on one host |
| Repos typically touched | 3–5 | 2–3 | 1 | 1 |
| Deployments that must roll together | BFF + every proto consumer + Limits + Payments | BFF + Payments | Payments | Payments |

The in-app study already showed legacy **files touched** at 20 vs 9 for AI-native. Chatty RPC multiplies that by the number of applications. Each extra repo is a cold start: different `Program.cs`, different DI, different error middleware, different test host.

Hypothetical fan-out for the chatty shape (not implemented here, on purpose):

```
limits-svc     Add DailyWithdrawalLimit column, Get/Set RPCs, proto vN
ledger-svc     SumTodayWithdrawals RPC (or the BFF loads transactions and sums)
payments-svc   Withdraw now calls Limits; new failure code
bff            New HTTP routes; map DailyLimitExceeded; orchestrate GetLimit + SumToday
web            Form + error string
contracts      NuGet/proto bump consumed by BFF and three services
```

That is the distributed twin of Domain exception + Application DTO + API contract + MediatR command. The agent’s context is no longer “too many types”; it is **too many systems**.

---

## What each extra RPC costs an agent

### 1. The use case is no longer in one place

Rule 6 (local reasoning) says a withdraw should live in a small folder. A BFF that calls `GetCustomer`, `GetBalance`, `GetDailyLimit`, `Charge`, `Debit` stores the **workflow** in the BFF and the **rules** in the callees. Neither side is complete:

- Change the limit check in Limits svc, miss the BFF call order → production bug.
- Change the BFF to skip `GetDailyLimit` for “performance” → rule silently disappears.
- Add `DailyLimitExceeded` only on the RPC → BFF still returns 500.

An end-to-end file is verbose on purpose. Distributed orchestration is verbose **and** incomplete in every file.

### 2. Contracts duplicate the same facts

Chatty RPC usually grows this stack for one field:

```
BFF WithdrawalRequest
  → proto WithdrawRequest
    → Payments WithdrawCommand
      → Domain Withdrawal
```

The in-app legacy app already did `WithdrawalRequest` vs `WithdrawalRequestDto`. Across applications the copies also have **independent version numbers**. The agent must know which consumer is on which proto, and whether `optional decimal daily_withdrawal_limit = 7` is backward compatible.

### 3. Partial failure is a new product

Seven sequential RPCs create states no single-process app has:

- Provider charged, debit failed
- Debit succeeded, transaction insert failed
- Limit read succeeded, later withdraw uses a stale limit
- BFF timeout, callee succeeded (double withdraw without idempotency)

The usual answer is sagas, outbox, and compensating transactions. Those are more applications, more topics, more “just in case” handlers. An agent implementing `DailyLimitExceeded` now has to ask whether a compensation path must know about the limit too.

End-to-end withdraw in one database transaction does not need that story. Keep the saga for things that are actually remote (the real Stripe), not for `GetBalance` on a database you own.

### 4. Tests lie at the boundary

The in-app write-up already showed mocked MediatR handlers missing the SQLite `DateTimeOffset` bug. Mocked RPC is the same failure at larger blast radius:

- BFF tests mock `ILimitsClient` → never see the UTC-day filter.
- Limits tests never see the BFF skipping the call on a code path.
- Contract tests, if they exist, often assert payload shape, not the **business** outcome.

AI-native HTTP tests against one host exercised the real query. Cross-app, the substitute is **one end-to-end test that boots the real callees** (or a modular monolith so there are no callees). A wall of mocked clients is not executable documentation of withdraw.

### 5. Search and ownership fight the agent

“Where is daily limit enforced?” In this repo the answer is `FEATURE-MAP.md` → `Withdraw.cs`. In a BFF estate the answer is a Slack argument: Limits team, Payments team, or “the BFF does it for the web only, mobile is different.”

Mobile BFF vs web BFF duplicating orchestration is the distributed form of “two ways to do it” (Rule 12). An agent given “fix withdraw limit on iOS” may patch the wrong BFF.

### 6. Runtime cost is real, but secondary

The in-app measurement found geo-mean p50 **0.640 ms** (AI-native) vs **0.752 ms** (legacy) on loopback — architecture was a few percent on top of SQLite. Chatty RPC is not a few percent. Each hop is:

- serialization + HTTP/2 or HTTP/1.1
- another process’s thread pool and DI
- tail latency of the slowest callee
- timeouts stacked (BFF 5s wrapping RPC 3s wrapping DB 2s)

That does not automatically justify a monolith. It does mean **splitting a withdraw into six RPCs buys neither speed nor AI maintainability**. You pay both.

---

## End-to-end: what “AI optimized” actually means here

End-to-end is not “one giant company binary.” It is **align the process boundary with the change boundary**.

| Change | Stays in one app | Worth a second app |
|--------|------------------|--------------------|
| Daily withdrawal limit | Yes — same aggregate as balance and withdrawals | No |
| Dummy Stripe/PayPal/Bank switch | Yes — it is a few lines in the operation | A **real** provider with its own SDK, webhooks, PCI scope: yes, behind a dedicated client |
| Web session cookie | Channel adapter | Yes, if you already have an identity host |
| “Payment completed” email | Outbox/event after commit | Notification svc, consumed asynchronously |
| Ledger vs customer profile owned by two orgs | — | Yes, but then withdraw **cannot** be a chatty BFF; it must be one API that is allowed to write both, or a defined distributed transaction you actually staff |

For this payment product, the AI-native topology is:

1. Public HTTP operations (`/deposits`, `/withdrawals`, `/balance`, daily-limit GET/PUT) served by **one** payments application.
2. Rules, persistence, and outcome mapping for each operation live together (in-app AI-native) or in a short handler+module pair (in-app balanced).
3. The browser or SPA calls that API directly, or through a **passthrough** that does not remap the body.
4. No second application exists whose only job is to call the first.

That last point is the BFF test: if the BFF’s withdraw method is “map JSON → RPC → map RPC → JSON,” delete the BFF method and let the client call payments. The BFF is not a capability; it is a hop.

---

## Other cross-application improvements that help AI

These apply even when you **must** have more than one deployable.

### 1. Slice by use case, not by noun

Prefer a Payments capability (deposit, withdraw, balance, daily limit) over Account + Transaction + Limit microservices. Noun services force every feature to be a distributed transaction. Use-case services match how agents and product tickets are worded: “add a daily withdrawal limit.”

### 2. One public contract per operation

Publish HTTP (or one RPC) whose body is the user intention. Do not publish `GetCustomer` + `GetBalance` + `GetLimit` for the withdraw **screen** and make every channel reassemble them.

If a screen needs a composite read, add an explicit **query operation** in the capability that owns the data (`GET /customers/{id}/withdrawal-page`), not a BFF that chatters.

### 3. Put a `SYSTEM-MAP.md` next to `FEATURE-MAP.md`

Agents already benefit from [`apps/3-ai-native/FEATURE-MAP.md`](apps/3-ai-native/FEATURE-MAP.md). At org scale, a root map should say:

| Use case | App / repo | Entry file | Owns tables | Downstream |
|----------|------------|------------|-------------|------------|
| Withdraw | `payments-api` | `Features/Withdraw.cs` | Customers, Transactions | dummy provider (in-process) |
| Send receipt | `notifications` | `Handlers/PaymentReceipt.cs` | none | mail provider; consumes `PaymentCompleted` |

If the map needs a sequence diagram for the **happy path**, the topology is already too hard. Sequence diagrams belong on the webhook/saga you cannot collapse.

### 4. Monorepo for code that ships together

A BFF repo + payments repo + proto repo means three clones, three CI definitions, and PRs that cannot be atomic. A monorepo (this repository’s shape: sibling apps, **no** shared library) lets the agent grep one tree. Keep isolation for **measurement** or for **independent deploy**, not as a default for every class library.

Do **not** “fix” duplication with a shared `Contracts` NuGet that every app references. That is a hidden coupling: one field change rebuilds the world, and the agent cannot see consumers without package version archaeology. Prefer:

- copy the small DTO at the edge (same stance as AI-native copy-not-share), or
- generate clients from **one** OpenAPI/proto checked in next to the owner, consumers regenerate.

### 5. One error vocabulary on the wire

This repo already shows the tax of two error models (legacy ProblemDetails vs `status` on HTTP 200). Across apps it gets worse: gRPC `FAILED_PRECONDITION`, BFF 422, frontend string.

Pick one closed set and keep it stable:

`Success | AccountNotFound | InsufficientFunds | InvalidAmount | UnknownProvider | ProviderDeclined | DailyLimitExceeded`

Map to HTTP (or gRPC) **once**, at the edge of the owning app. Downstream adapters must not invent a second enum.

### 6. Async only after the money decision

```
Withdraw (sync, one app, one transaction)
  → commit
  → publish PaymentCompleted (at-least-once, outbox)
      → notifications
      → analytics
```

Do not put Limits or Ledger on that bus for the withdraw itself. Event-driven withdraw is a saga by another name. Agents handle “if this, then that” in one function; they handle “eventually, unless the consumer lagged” poorly.

### 7. Contract tests against the owner, not mocks of yourself

If a second app must call payments:

- consumer-driven contract tests (Pact, schema snapshots) against the **real** OpenAPI
- one black-box withdraw test in payments that remains the source of truth
- no `IPaymentsClient` fake inside the BFF that returns `Success` for every input

The SQLite date bug is the cautionary tale: the mock will not save you.

### 8. Identity and money are different lifetimes; UI and money often are not

Split **identity** (login, tokens, user directory) from **payments** when they scale and staff independently. Do not split **web BFF** from **payments** if the BFF’s only logic is field renaming. Channel adapters may exist; they should be thinner than `apps/2-balanced` handlers, not thicker.

### 9. Stop translating at every hop

Each of JSON ↔ proto ↔ DTO ↔ domain entity is a file the agent must prove equivalent. End-to-end HTTP + EF on a record type removes three of those. If you need gRPC for an internal caller, generate it from the same operation model; do not hand-write a parallel type system.

### 10. Idempotency and correlation as request fields

Cross-app retries will happen. Make them visible:

```http
POST /withdrawals
Idempotency-Key: 9f3c…
```

An agent can see that a second call is safe. Hidden “maybe retry in the BFF” is global knowledge (Rule 3).

### 11. Deploy together what you change together

If BFF and payments always release on the same day for the same ticket, they are one application wearing two Dockerfiles. Either merge them or accept that `DailyLimitExceeded` needs a compatibility window (old BFF, new enum — what HTTP status?). Compatibility windows are extra branches for the agent to reason about.

### 12. Observability does not replace local reasoning

Trace IDs help operators after an incident. They do not reduce the files an agent must load **before** the change. Prefer collapsing hops over instrumenting them. Keep traces for the hops that remain (real provider, real bank file).

### 13. Gateways are pipes, not composers

YARP/NGINX/API Management may terminate TLS and route `/payments/*` to the payments app. The moment the gateway aggregates Limits + Ledger, it becomes a BFF written in YAML — worse for AI, because the workflow is not in C# and not in the test project.

### 14. Frontends call operations, not graphs of entities

A GraphQL BFF that stitches Account, Limit, and Transaction resolvers is chatty RPC with a schema. The agent must load resolvers **and** every subgraph. A withdraw mutation that resolves entirely in the payments subgraph is fine; a withdraw mutation that calls four subgraphs is the legacy topology.

### 15. Keep agent docs at the repo root

This repository already has `SPEC.md`, plus `AGENTS.md` and `FEATURE-MAP.md` on the AI-native app. Legacy and balanced are counter-examples and do not have agent maps. For multiple AI-oriented apps, add:

- `SPEC.md` — product behavior (exists)
- `SYSTEM-MAP.md` — which app owns which operation
- per-app `AGENTS.md` — how to add an operation **in that app**

The agent should not infer topology from folder folklore.

---

## Decision table

| Situation | Topology to use | Why it is cheaper for AI |
|-----------|-----------------|--------------------------|
| New field or rule on withdraw/balance/limit | End-to-end payments app (this repo’s shape) | One process, one test host, local reasoning |
| Web/mobile need different **auth** | Thin channel adapter + same payments HTTP | Adapter has no business rules to duplicate |
| Web/mobile need different **screens** | Query operations on payments, or BFF that only reshapes **reads** it does not own writes for | Writes stay in one place |
| Real external provider (Stripe) | Payments app calls one provider client; webhooks in the same app or a narrowly scoped worker | External I/O is explicit; not an internal RPC mesh |
| Notifications, ETL, search index | Events after commit | Failure there must not roll back the withdraw the user already saw |
| Two domains that rarely co-change (Identity vs Payments) | Two apps, **coarse** APIs (`GetSubject`, not seven RPCs per page) | Change boundary matches service boundary |
| Team topology is already noun-shaped (Limits team, Ledger team) | Still prefer one withdraw API; Limits team contributes a **module** or a library until scale forces a process | Conway’s law is not an RPC requirement |

---

## Mapping to the in-app principles

| Principle | In-app (already measured) | Cross-app |
|-----------|---------------------------|-----------|
| Minimize context per change | Fewer files in one solution | Fewer solutions in the path |
| Local reasoning | Vertical slice / one file | Use case owned by one runtime |
| Few abstractions | No MediatR/UoW/factory | No BFF façade + proto + client + handler |
| Shallow dependency graph | API → handler → DB | Client → payments → DB |
| One obvious way | One status union, one data access | One public operation, one owner |
| Tests as documentation | HTTP tests, not mocks | Contract + HTTP tests against the owner, not mocked RPC |
| Explicit errors | Closed `Outcome` | Same union across the wire; no dual HTTP+gRPC folklore |
| Boring beats clever | Copy-paste over shared helpers | Copy or generate contracts; no shared kernel package maze |

The balanced **in-app** style (handlers + modules) is still the cheapest **token** shape once the agent knows the modules. The balanced **cross-app** style is the same idea: modules, not microservices, until a real boundary appears.

The AI-native **in-app** style spends tokens **inside** `Withdraw.cs` so the agent does not hunt. The AI-native **cross-app** style spends those tokens in **one** application so the agent does not hunt across a mesh.

---

## What this repository already got right

The three apps disagree internally, but they agree on topology:

- No BFF in front of the payment API
- No RPC between deposit, withdraw, balance, and daily limit
- No shared class library contaminating measurements
- One SQLite file **per app** (no shared database pretending to be integration)
- A single [`SPEC.md`](SPEC.md) as the product contract

That is why the daily-limit experiment could be a fair in-process comparison. Putting a BFF and a proto repo in front of `apps/1-legacy` would have dwarfed the 20-vs-9 files-touched gap. The largest AI maintainability win available around these apps is **not** another in-process refactor; it is **keeping the use case end-to-end** as the product grows.

---

## Recommendations (do not implement here)

These are advisory. This analysis did not change `apps/`.

1. **Keep payments end-to-end.** New product rules (limits, fees, holds) belong in the same application that already owns `Customers` and `Transactions`, not in a new Limits microservice called from a BFF.
2. **If a BFF is introduced, forbid business orchestration.** Auth and header mapping only. Withdraw stays `POST /withdrawals` on the payments app with the same body.
3. **Never split one user action into multiple internal RPCs** (“get / get / get / command”). That is the highest AI tax per feature in this document.
4. **Add `SYSTEM-MAP.md` when a second deployable appears.** Until then, `FEATURE-MAP.md` is enough.
5. **Preserve one closed outcome set** on the public API. Do not let a future gateway re-map `DailyLimitExceeded` into a generic 400.
6. **Prefer a modular monolith (balanced) or operation files (AI-native) over a service mesh** for anything that co-changes with withdraw.
7. **Treat real providers and real identity as the first justified process boundaries**, not Account vs Ledger vs Limit.

The in-app study ranked **files touched** AI-native < balanced < legacy, and **tokens** balanced < AI-native < legacy. Across applications the ranking is simpler: **end-to-end << one RPC behind a BFF << chatty RPC.** Chatty RPC is the legacy app’s hop chain, moved onto the network, where the compiler can no longer save the agent.
