---
name: evidence-driven-debugging
description: Diagnose and fix MiniCore test failures, runtime errors, crashes, performance regressions, and bug reports through an evidence-first loop. Use whenever a test, Player build, device run, log, report, or user reports an error; require log analysis and targeted instrumentation before making an unproven fix, then document the verified repair and regression procedure.
---

# Evidence-Driven Debugging

Use this workflow for every test or bug change that encounters an error, failure, crash, unexpected result, or performance regression.

## 1. Preserve and read evidence first

- Collect the exact failing command, test name, relevant logs/stack trace, report rows, build variant, device/transport configuration, and timing.
- Read the failure from the first meaningful error through the causal call chain. Do not infer a cause from the final symptom alone.
- Map each observed boundary to its owner: caller, queue, transport, receive loop, dispatcher, handler, persistence/UI, or test measurement.
- State separately: confirmed facts, ruled-out causes, and open hypotheses.

## 2. Decide whether the cause is already proven

Apply a direct minimal fix only when logs, source, and reproduction identify both:

1. the exact faulty boundary or condition; and
2. a solution whose effect follows directly from that evidence.

Do not change thread ownership, queue limits, batching, retry policy, timeout, or architecture solely because it might improve the symptom.

## 3. Instrument an uncertain boundary

When evidence shows where a failure appears but not what happens before, inside, or after that point:

- Add narrowly scoped diagnostics on both sides of the boundary and, where needed, inside it.
- Record counts and timestamps sufficient to distinguish accepted, rejected, started, completed, timed out, duplicated, ignored, and failed work.
- Include correlation IDs or sequence numbers where an item crosses asynchronous boundaries.
- Keep diagnostics bounded: gate them to the test/debug mode, rate-limit text logs, prefer counters/snapshots over per-item logs in hot paths, and avoid changing production behavior.
- State exactly what result would confirm or refute each hypothesis.

## 4. Reproduce and iterate

- Re-run the same minimal failing test/configuration first, then compare it with the prior artifact.
- If the boundary is still uncertain, refine diagnostics and repeat from section 1.
- Once the evidence proves the cause, remove temporary noisy logging, retain useful low-cost metrics, implement the smallest correct fix, and run focused regression tests before broader tests.
- Never present a hypothesis, an optimization experiment, or a successful build as a verified root-cause fix.

## 5. Verify and document the finished repair

Do not call a bug resolved until the failure is reproduced as fixed by the relevant test(s) and the result has been checked for regressions proportionate to risk.

When the repair is verified, use the project documentation-maintenance skill and update the appropriate documentation with a newest-first record containing:

- symptom and reproduction command/configuration;
- original evidence and confirmed root cause;
- fix and its scope;
- focused and broader regression tests, with results;
- remaining limitations, follow-up work, and any diagnostic metric intentionally retained.

For performance work, archive the input report location and before/after values. For device tests, provide copyable commands for the next required run.
