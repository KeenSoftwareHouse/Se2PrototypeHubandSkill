# Pitch / design doc (optional last step)

Only when the user asks for one. Readers are designers and players, not programmers: lead with what
the player experiences and why, in plain words with examples. Engine internals, code names and
maths stay out of the body. If technical detail matters, write a separate "Technical Notes" doc.

Layout that works well for prototype pitches:

```
FEATURE NAME IN CAPS
One line: what this is for.

OVERVIEW
- 3-6 bullets: the player problem, what the feature does, how it feels.

SECTION HEADINGS IN CAPS
- Short rule bullets. One idea per bullet.
- Concrete examples ("Hold P near a connector: the ship lines up and docks").

Edge Cases:
- What happens when ... ?

STARTING NUMBERS
- Value, with * marking values that are tunable in data.
```

Keep it punchy. Prototype-proven facts ("in the prototype, a 2 t fighter docks in ~4 s") beat
speculation. Put tuning numbers in STARTING NUMBERS, not in the prose.

If a document connector is available, create it there; otherwise write a `.md` next to the project.
