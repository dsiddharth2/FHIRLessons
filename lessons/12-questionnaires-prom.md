# Lesson 12: Questionnaires — Patient Reported Outcome Measures

## Overview

So far every resource we've created has been clinician-authored — a doctor records a
diagnosis, a nurse logs vitals, a system tracks procedures. But healthcare increasingly
values the **patient's own voice**: How much pain are you in? Can you walk up stairs?
How's your mood?

These are **Patient Reported Outcome Measures (PROMs)** — standardized surveys that
patients fill out themselves. FHIR models them with two resources:

- **Questionnaire** — the form definition (questions, answer types, rules)
- **QuestionnaireResponse** — the patient's actual answers

This lesson covers the Questionnaire resource. The next lesson will cover
QuestionnaireResponse.

---

## What Is a Questionnaire?

A Questionnaire is a structured form definition. Think of it as a template — it defines
what questions to ask, what types of answers to accept, and any validation rules. It
doesn't contain anyone's answers.

### Real-World Examples

| Questionnaire | Domain | Questions |
|---------------|--------|-----------|
| PHQ-9 | Depression | 9 questions, scored 0-3 each |
| GAD-7 | Anxiety | 7 questions, scored 0-3 each |
| PROMIS-10 | General health | 10 questions covering physical/mental health |
| VR-12 | Veterans health | 12 questions, physical + mental component |

### Questionnaire vs. Other Resources

| Resource | Who creates it | What it captures |
|----------|---------------|-----------------|
| Observation | Clinician/device | A single measurement (e.g., BP = 130/85) |
| Questionnaire | Form designer | The form template (questions + answer rules) |
| QuestionnaireResponse | Patient | The patient's answers to a specific Questionnaire |

---

## Questionnaire Structure

A Questionnaire has a flat-or-nested list of **items**. Each item is a question (or a
group of questions).

```
Questionnaire
  ├── item[0] (group: "General Health")
  │     ├── item[0.0] (question: "Rate your overall health")
  │     └── item[0.1] (question: "How many days were you physically active?")
  ├── item[1] (group: "Pain Assessment")
  │     ├── item[1.0] (question: "Rate your pain level 0-10")
  │     └── item[1.1] (question: "Where is the pain located?")
  └── item[2] (question: "Additional comments")
```

### Item Types

| Type | Description | Example |
|------|-------------|---------|
| `group` | Container for other items (no answer itself) | "Section 1: General Health" |
| `display` | Read-only text shown to the user | "Please answer the following..." |
| `boolean` | Yes/No | "Do you smoke?" |
| `integer` | Whole number | "How many days per week do you exercise?" |
| `decimal` | Number with decimals | "What is your weight in kg?" |
| `string` | Free text (short) | "Describe your symptoms" |
| `text` | Free text (long, multi-line) | "Additional comments" |
| `choice` | Pick from a list | "Rate: Excellent / Good / Fair / Poor" |
| `date` | Calendar date | "When did symptoms start?" |
| `dateTime` | Date and time | "When was your last meal?" |
| `quantity` | Number with a unit | "Temperature: ___ °F" |

### Answer Options for Choice Items

Choice items need a list of valid answers. FHIR provides two ways:

1. **answerOption** — inline list of options directly in the item
2. **answerValueSet** — reference to an external ValueSet containing the options

For learning, we'll use `answerOption` (simpler, self-contained).

---

## Key Questionnaire Fields

| Field | Required? | Description |
|-------|-----------|-------------|
| `url` | YES (canonical) | Globally unique identifier for this questionnaire |
| `status` | YES | draft / active / retired / unknown |
| `title` | Recommended | Human-readable name |
| `name` | Recommended | Computer-friendly name (no spaces) |
| `version` | Recommended | Version string (e.g., "1.0.0") |
| `subjectType` | Recommended | What resource type this applies to (e.g., Patient) |
| `item` | YES | The questions/groups |

### Item Fields

| Field | Required? | Description |
|-------|-----------|-------------|
| `linkId` | YES | Unique ID within the questionnaire (used to link answers) |
| `text` | YES (for non-group) | The question text |
| `type` | YES | One of the item types above |
| `required` | No | Is an answer mandatory? (default: false) |
| `repeats` | No | Can the question be answered multiple times? (default: false) |
| `answerOption` | For choice | List of allowed answers |
| `answerValueSet` | For choice | Reference to a ValueSet of allowed answers |
| `maxLength` | For string/text | Maximum character length |
| `initial` | No | Default answer value |

---

## Our PROM Questionnaire

We'll create a questionnaire called "PRoM Test Questionnaire" that covers common
patient-reported outcomes:

1. **General Health** (group)
   - Overall health rating (choice: Excellent/Very Good/Good/Fair/Poor)
   - Days physically active per week (integer, 0-7)

2. **Pain Assessment** (group)
   - Current pain level (integer, 0-10)
   - Pain interferes with daily activities? (boolean)

3. **Mental Health** (group)
   - Feeling down or depressed in last 2 weeks? (choice: frequency scale)
   - Feeling anxious or nervous in last 2 weeks? (choice: frequency scale)

4. **Additional comments** (text, optional)

This is simplified compared to validated instruments like PHQ-9, but it demonstrates
all the key Questionnaire concepts.

---

## Coding Systems for Questionnaires

| System | Use |
|--------|-----|
| `http://loinc.org` | Standard questionnaire/question codes (e.g., PHQ-9 = 44249-1) |
| `http://snomed.info/sct` | Clinical concepts referenced in questions |
| Custom URL | Your organization's questionnaire identifier |

For standardized instruments, each question has a LOINC code. For custom questionnaires,
you define your own canonical URL.

---

## What Our Code Will Do

We'll create a `QuestionnaireService` that:

1. **Creates a PROM Questionnaire** — builds the full Questionnaire resource with groups,
   questions, answer options, and required flags
2. **Reads a Questionnaire** — fetches by ID
3. **Searches Questionnaires** — finds questionnaires by title, URL, or status

The service demonstrates how to construct nested item hierarchies, define answer options
for choice items, and set validation constraints (required, integer ranges).

---

## FHIR Operations on Questionnaire

| Operation | Description |
|-----------|-------------|
| `POST /fhir/Questionnaire` | Create a new questionnaire |
| `GET /fhir/Questionnaire/{id}` | Read a specific questionnaire |
| `GET /fhir/Questionnaire?title=PRoM` | Search by title |
| `GET /fhir/Questionnaire?url=...` | Search by canonical URL |
| `GET /fhir/Questionnaire?status=active` | Search by status |

---

## Questionnaire Lifecycle

```
draft  ──>  active  ──>  retired
              │
              └── (create QuestionnaireResponses against active questionnaires)
```

- **draft** — still being designed, not ready for use
- **active** — published and ready for patients to fill out
- **retired** — no longer in use, but historical responses still reference it

You should only create QuestionnaireResponses against `active` questionnaires. Retired
questionnaires are kept so existing responses remain valid.

---

## How Questionnaire Connects to Other Resources

```
Questionnaire (form definition)
   │
   ├── QuestionnaireResponse (patient's answers)
   │     ├── subject: Patient/1
   │     └── encounter: Encounter/2
   │
   └── PlanDefinition (care protocol may require this questionnaire)
         └── CarePlan (patient-specific plan references the activity)
```

The Questionnaire is the template. QuestionnaireResponse captures answers.
PlanDefinition can reference the Questionnaire as a required activity in a care protocol.

---

## Common Questions

**Q: Can a Questionnaire have conditional logic (show question B only if A = yes)?**
Yes — FHIR supports `enableWhen` on items. You can show/hide questions based on
answers to other questions. We're not using it in this lesson but it's a powerful feature
for real-world forms.

**Q: What's the difference between `answerOption` and `answerValueSet`?**
`answerOption` puts the choices inline in the Questionnaire JSON. `answerValueSet`
references a separate ValueSet resource. Use answerOption for small, questionnaire-specific
lists. Use answerValueSet when the same options are shared across multiple questionnaires
or when you want terminology server validation.

**Q: Can I score a questionnaire automatically?**
FHIR supports extensions for scoring (like `ordinalValue` on answer options), and you
can compute scores in your application. Some IGs define scoring profiles. HAPI doesn't
auto-score — that's application logic.

**Q: Does HAPI validate QuestionnaireResponses against the Questionnaire?**
HAPI can validate that answers match the expected types and that required questions are
answered, but this depends on configuration and the `$validate` operation. We'll explore
this in the next lesson.

---

## Summary

- **Questionnaire** defines a form — questions, answer types, structure, and rules
- Items can be **nested** (groups contain questions) or **flat**
- Each item has a **linkId** (unique key) and a **type** (boolean, choice, integer, etc.)
- Choice items use **answerOption** (inline) or **answerValueSet** (external reference)
- Questionnaires have a **lifecycle**: draft → active → retired
- PROMs are standardized patient surveys — PHQ-9, GAD-7, PROMIS-10, etc.
- The Questionnaire is just the template — patient answers go in **QuestionnaireResponse**

## Next Step

Proceed to **Lesson 13** — create a QuestionnaireResponse that captures Patient/1's
answers to the PRoM Test Questionnaire built in this lesson.
