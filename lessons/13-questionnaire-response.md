# Lesson 13: QuestionnaireResponse — Capturing Patient Answers

## Overview

In Lesson 12 we built a **Questionnaire** — the form template with questions, answer
types, and required flags. But a blank form isn't useful on its own. This lesson is about
**QuestionnaireResponse** — the resource that records what a patient actually answered.

Think of it this way:
- **Questionnaire** = the exam paper
- **QuestionnaireResponse** = the student's filled-in answer sheet

---

## What Is a QuestionnaireResponse?

A QuestionnaireResponse captures one patient's answers to one specific Questionnaire at
a specific point in time. It mirrors the Questionnaire's item structure — each answer
item uses the same `linkId` as the corresponding question.

```
Questionnaire (PRoM Test)                QuestionnaireResponse
  item[linkId=1.1] "Rate health"    →      item[linkId=1.1] answer: "Good"
  item[linkId=1.2] "Days active"    →      item[linkId=1.2] answer: 4
  item[linkId=2.1] "Pain level"     →      item[linkId=2.1] answer: 3
  item[linkId=2.2] "Pain interferes"→      item[linkId=2.2] answer: false
  ...                                      ...
```

The `linkId` is the glue — it's how you match an answer back to its question.

---

## Key QuestionnaireResponse Fields

| Field | Required? | Description |
|-------|-----------|-------------|
| `questionnaire` | YES | Canonical URL of the Questionnaire being answered |
| `status` | YES | in-progress / completed / amended / entered-in-error / stopped |
| `subject` | Recommended | Who the answers are about (usually a Patient reference) |
| `authored` | Recommended | When the response was completed |
| `author` | No | Who filled it out (Patient, Practitioner, or RelatedPerson) |
| `encounter` | No | The Encounter during which this was filled out |
| `source` | No | Who provided the answers (if different from author) |
| `item` | YES | The answers, mirroring the Questionnaire structure |

### Response Item Fields

| Field | Description |
|-------|-------------|
| `linkId` | Must match the Questionnaire item's linkId |
| `text` | The question text (optional but helpful for readability) |
| `answer` | List of answers (usually one, but can be multiple if `repeats` is true) |
| `answer.value[x]` | The actual answer — type depends on the question type |
| `item` | Nested items (for groups) |

---

## Answer Types Map to Question Types

| Question Type | Answer Value Type | Example |
|---------------|------------------|---------|
| `boolean` | `valueBoolean` | `true` / `false` |
| `integer` | `valueInteger` | `4` |
| `decimal` | `valueDecimal` | `98.6` |
| `string` | `valueString` | `"Mild headache"` |
| `text` | `valueString` | `"No major concerns..."` |
| `choice` | `valueCoding` | `{ code: "good", display: "Good" }` |
| `date` | `valueDate` | `"2026-04-18"` |
| `dateTime` | `valueDateTime` | `"2026-04-18T14:30:00Z"` |
| `quantity` | `valueQuantity` | `{ value: 72, unit: "kg" }` |

---

## QuestionnaireResponse Lifecycle

```
in-progress  ──>  completed  ──>  amended
                      │
                      └──>  entered-in-error
```

- **in-progress** — patient is still filling it out
- **completed** — all answers submitted, final
- **amended** — answers were changed after completion
- **entered-in-error** — the whole response was a mistake, should be ignored
- **stopped** — patient started but didn't finish, and won't continue

Most responses go directly to `completed` when submitted in one sitting.

---

## The Questionnaire Reference

A QuestionnaireResponse links to its Questionnaire via the `questionnaire` field.
This uses the **canonical URL** (not a relative reference like `Questionnaire/123`):

```json
"questionnaire": "http://example.org/fhir/Questionnaire/prom-test"
```

This canonical reference means the response is tied to the *definition* of the form,
not a specific server instance. The same Questionnaire URL works across different
FHIR servers.

---

## Our Patient's Answers

We'll record that Patient/1 (John Smith) completed the PRoM Test Questionnaire with
these answers:

| Question | LinkId | Answer |
|----------|--------|--------|
| Overall health rating | 1.1 | Good |
| Days physically active | 1.2 | 4 |
| Pain level (0-10) | 2.1 | 3 |
| Pain interferes with activities | 2.2 | false (No) |
| Feeling depressed | 3.1 | Several days |
| Feeling anxious | 3.2 | Not at all |
| Additional comments | 4 | "Occasional mild headache in the evenings" |

This tells a clinical story: the patient is in generally good health, moderately
active, with mild pain, some depressive symptoms, and no anxiety.

---

## How QuestionnaireResponse Connects

```
Questionnaire (PRoM Test — the form template)
   │
   └── QuestionnaireResponse (John Smith's answers)
         ├── questionnaire: "http://example.org/fhir/Questionnaire/prom-test"
         ├── subject: Patient/1
         ├── encounter: Encounter/2 (optional — during which visit?)
         └── item[] (mirrors Questionnaire structure with answers)
```

In a clinical workflow:
1. A care plan says "patient must complete PRoM survey"
2. Patient fills out the form → creates a QuestionnaireResponse
3. Clinician reviews the answers during the next visit
4. Answers may trigger follow-up actions (e.g., PHQ-9 score > 10 → depression screening)

---

## Searching QuestionnaireResponses

| Search | URL | Use |
|--------|-----|-----|
| By patient | `?subject=Patient/1` | All responses for a patient |
| By questionnaire | `?questionnaire=http://example.org/...` | All responses to a specific form |
| By status | `?status=completed` | Only completed responses |
| By date | `?authored=ge2026-04-01` | Responses after a date |
| Combined | `?subject=Patient/1&questionnaire=...` | Specific patient + specific form |

---

## What Our Code Will Do

We'll create a `QuestionnaireResponseService` that:

1. **Creates a completed response** — builds a QuestionnaireResponse with answers for
   all questions in the PRoM questionnaire
2. **Reads a response** — fetches by ID
3. **Searches responses** — by patient and by questionnaire URL

---

## Common Questions

**Q: Does the server validate answers against the Questionnaire?**
HAPI can validate that linkIds match and answer types are correct if you use `$validate`.
But by default, it stores whatever you send. Strict validation depends on configuration
and whether the Questionnaire is loaded on the server.

**Q: Can a patient answer the same Questionnaire multiple times?**
Yes — each submission creates a new QuestionnaireResponse. This is common for repeated
assessments (e.g., monthly PHQ-9 scores to track depression over time). You search by
patient + questionnaire + date to find specific responses.

**Q: What if the patient skips optional questions?**
Simply omit the item from the response. Only required questions need answers. The
`status` can still be `completed` if all required questions are answered.

**Q: Can someone other than the patient fill it out?**
Yes — use `author` to record who filled it out (could be a caregiver or clinician),
and `source` if the information came from someone else. For PROMs, typically the
patient is both author and source.

**Q: How do I calculate a score from the answers?**
Scoring is application logic, not built into the resource. You'd read the response,
extract the answer values, and compute a score based on the instrument's scoring rules
(e.g., PHQ-9 sums the 0-3 values for each question, max score 27).

---

## Summary

- **QuestionnaireResponse** captures one patient's answers to one Questionnaire
- Each answer item uses the same **linkId** as the question it answers
- The `questionnaire` field uses the **canonical URL** (not a server-relative reference)
- Answer types match question types: choice → valueCoding, integer → valueInteger, etc.
- Responses have a lifecycle: in-progress → completed → amended
- Multiple responses to the same Questionnaire are normal (repeated assessments)

## Next Step

Proceed to **Lesson 14** — create a PlanDefinition that defines a care protocol
requiring the patient to complete the PRoM survey.
