# Bootstrap Prompt for Claude Code

Paste this as your first message to Claude Code after `cd C:\Users\danvi\source\repos\tossaway`:

---

Read `SPEC.md` in full before doing anything else.

This is a throwaway Blazor Server POC for a spreadsheet-like grid with named-reference formulas. Do not skim. The spec is the source of truth and explicit about what is in scope and what is not.

Once you've read it, do NOT start writing code. Instead:

1. Summarize the architecture back to me in 5-10 bullets to confirm you understand it.
2. Call out any ambiguities or decisions the spec leaves open that you think need resolution before step 1 of the implementation order.
3. Confirm the .NET 9 SDK scaffolding command you plan to use and the NCalc package version you'll target.
4. Wait for my approval before running any commands or writing any files.

After approval, follow the Implementation Order section strictly. Stop after each numbered step and confirm it builds and runs before proceeding to the next. If you encounter a choice the spec doesn't cover, stop and ask — do not improvise.

Do not add anything in the "Guardrails" or "Non-Goals" sections of the spec, even if it seems helpful.
