# Setup flow: any other assistant

The skill is plain files, and you follow them directly. Do **not** run `install.ps1` or
`scripts\install-agents.ps1`: they only register the skill with one particular tool and would
leave unused folders on the user's PC.

## 1. Make the skill easy to find again

The instructions live in `prototype-engineers\SKILL.md` in the skill's home folder (one level up
from this file). So the user doesn't have to point you at it every time, offer to add one line to
wherever **your** tool keeps standing instructions for the user (its global rules, memory or
instructions file - you know where that is for your tool; ask the user if you don't):

> For Space Engineers 2 prototypes, read and follow `<skill home>\prototype-engineers\SKILL.md`.

Write the real folder path in place of `<skill home>`. Only add it if the user says yes. If your
tool has no such place, tell the user to start future requests with "follow the PrototypeEngineers
skill in `<skill home>`".

## 2. How to read SKILL.md

`SKILL.md` was written in one tool's terms. Everything in it applies to you, with these swaps:

| SKILL.md says | You do |
|---|---|
| "this skill's folder" / `<skill>`, and every `scripts\...` or `references\...` path | Relative to `<skill home>\prototype-engineers`. From the skill home, run scripts as `prototype-engineers\scripts\<name>.ps1`. |
| "Before anything: is this skill current?" | Run `prototype-engineers\scripts\check-skill-update.ps1`. On "BEHIND", after the user agrees, run `git pull` in the skill home and re-read `SKILL.md`; skip the `install.ps1` part. |
| Environment step 5, "Helper agents" / `install-agents.ps1` | Skip it. |
| Hand a job to `pe-api-scout`, `pe-log-reader`, `pe-build-fixer` or `pe-reviewer` | If your tool can start a sub-agent or a cheaper model for a side job, give it the body of `agents\<name>.md` followed by the question. Otherwise do the job yourself, following that file's rules. |
| Model names and token costs in "Cheaper helpers" | Ignore the numbers; keep the idea: bulky, checkable reading (decompiled code, long logs) is worth handing off or keeping short, because it stays in your context. |
| "AskUserQuestion" | Ask the user in the chat and wait for the answer. |
| "next session" / "new session" | Whatever starts a fresh conversation in your tool. |
| `plugin.json` field `builtWith` (the template's default text) | Ask the user what to write; suggest the name of the tool you are. Never put your own name in `author`. |

Everything else - the scripts, the project template, the rules in sections 3 to 5, the API
cookbook, where discoveries are saved - is tool-independent. Follow it exactly.

## 3. Go back to `SETUP.md` step 2
