# LLM Meal Planner Pipeline

## Design Document

Author: Gwynneth Uhrhammer via ChatGPT

---

# 1. Project Overview

## Goal

Build a multi-stage LLM pipeline that generates meal plans, recipes, grocery lists, and nutrition summaries from natural language user constraints.

The primary purpose of this project is NOT to build a production-ready meal planning application.

The primary purpose is to deeply understand:

- AI output evals
- Multi-stage LLM orchestration
- Debugging chained LLM systems
- Stage isolation and observability
- Structured contracts between AI components
- Eval-driven retries and iteration
- Failure analysis in AI pipelines

This project is specifically designed as preparation for interviews involving:

- AI systems design
- Multi-model pipelines
- Evaluations ("evals")
- Debugging ambiguous AI behavior
- Production AI engineering

The architecture intentionally emphasizes:

- Structured outputs
- Deterministic validation
- Traceability
- Isolation of failures
- Observability
- Incremental debugging

---

# 2. Why This Project Exists

A major theme in modern AI systems engineering is:

> When a complex AI pipeline produces bad output, how do you determine WHICH stage failed with confidence?

This project is designed to exercise exactly that problem.

Instead of treating an LLM as a magical black box, this project treats AI components as engineering systems that:

- have contracts
- can be evaluated independently
- can fail independently
- can be isolated during debugging
- can be benchmarked
- can be retried and improved

The project intentionally uses MULTIPLE LLM stages because failure propagation becomes significantly harder to reason about.

Example:

- Did the meal planner violate dietary constraints?
- Did the recipe generator introduce invalid ingredients?
- Did the grocery aggregator lose data?
- Did a downstream stage amplify an upstream mistake?

The core engineering challenge is:

> Building enough instrumentation and eval infrastructure to identify root causes quickly.

---

# 3. High Level System Architecture

```text
User Constraints
   ↓
Meal Planning Stage
   ↓
Recipe Generation Stage
   ↓
Grocery Aggregation Stage
   ↓
Nutrition Summary Stage (optional)
```

Each stage:

- receives structured input
- produces structured JSON output
- has independent evals
- can be run in isolation
- can be replaced with known-good test data

This separation is CRITICAL.

The pipeline should NEVER rely solely on end-to-end validation.

---

# 4. Technology Decisions

## Language Choice: C#

The project will be implemented in C# rather than Python.

Reasons:

- Strong familiarity with C# reduces cognitive overhead
- Strong typing encourages explicit contracts
- Easier to reason about architecture and validation
- Better alignment with systems engineering interview discussions
- Better support for interfaces and structured orchestration
- Easier to discuss production-quality engineering patterns

This project is fundamentally:

- a systems orchestration problem
- an observability problem
- an evaluation problem

It is NOT primarily an ML training project.

Therefore, Python-specific ML ecosystem advantages are less important.

---

## LLM Provider Strategy

LLM providers should be abstracted behind interfaces.

This allows swapping:

- Ollama
- OpenAI
- Claude API
- local models
- future providers

without changing orchestration logic.

Example interface:

```csharp
public interface ILlmClient
{
    Task<string> GenerateAsync(string prompt);
}
```

---

## Initial LLM Recommendation

Primary recommendation:

- Ollama
- Local open-source models

Potential models:

- Llama 3
- Mistral
- Qwen

Reasons:

- Cheap experimentation
- Easy iteration
- Faster eval loops
- No API cost concerns during development

Optional:

- Add OpenAI/Claude later as comparison baselines

---

# 5. Core Engineering Principles

## Principle 1: Structured Contracts

Every stage MUST output structured JSON.

Avoid freeform text whenever possible.

This enables:

- deterministic validation
- schema checking
- isolated evals
- reliable downstream parsing

---

## Principle 2: Per-Stage Evals

Every stage must have independent evaluation.

Do NOT rely only on:

"does the final output look good?"

Instead:

- validate stage outputs individually
- score each stage independently
- isolate failures quickly

---

## Principle 3: Input Substitution

The system must support replacing any stage output with known-good data.

Example:

- Replace generated recipes with hardcoded recipes
- Re-run grocery aggregation
- Determine whether the grocery stage is actually the problem

This is one of the MOST IMPORTANT concepts in the project.

---

## Principle 4: Observability

The system should log:

- prompts
- outputs
- eval results
- retry attempts
- latency
- parse failures
- schema violations

Every pipeline run should be inspectable.

---

## Principle 5: Eval-Driven Iteration

Stages should be retryable.

If evals fail:

- generate feedback
- retry stage
- compare improvement

Example:

"This recipe violates vegetarian constraints because it contains chicken. Regenerate the recipe while remaining vegetarian."

---

# 6. Project Phases

The project should be built incrementally.

DO NOT attempt to build everything at once.

---

# Phase 1 — Basic Pipeline

## Goal

Create the minimal functioning multi-stage system.

## Scope

Implement:

- meal planning stage
- recipe generation stage
- grocery aggregation stage
- JSON contracts
- sequential orchestration

No retries.
No advanced evals.
No UI.

---

## Example User Input

```text
Plan 5 vegetarian dinners under 600 calories each with minimal prep time.
```

---

## Stage 1 — Meal Planner

### Input

```json
{
  "diet": "vegetarian",
  "maxCalories": 600,
  "days": 5,
  "maxPrepTimeMinutes": 30
}
```

### Output

```json
{
  "days": [
    {
      "day": "Monday",
      "meal_name": "Chickpea Spinach Curry",
      "diet_tags": ["vegetarian"],
      "estimated_calories": 550
    }
  ]
}
```

---

## Stage 2 — Recipe Generator

Runs once per meal.

### Output

```json
{
  "meal_name": "Chickpea Spinach Curry",
  "ingredients": [
    {
      "name": "chickpeas",
      "quantity": 2,
      "unit": "cups"
    }
  ],
  "steps": [
    "Heat oil",
    "Add spices"
  ],
  "estimated_calories": 550
}
```

---

## Stage 3 — Grocery Aggregator

### Output

```json
{
  "items": [
    {
      "name": "chickpeas",
      "total_quantity": 4,
      "unit": "cups"
    }
  ]
}
```

---

## Deliverables

- Functional console application
- End-to-end pipeline execution
- Structured outputs
- Basic logging

---

# Phase 2 — Deterministic Evals

## Goal

Add independent validation per stage.

This phase is CRITICAL.

---

## Meal Plan Evals

Checks:

- correct number of meals
- calorie limits respected
- dietary tags present
- no duplicate meal names

Example result:

```json
{
  "passed": true,
  "score": 0.9,
  "issues": []
}
```

---

## Recipe Evals

Checks:

- ingredients list non-empty
- steps list non-empty
- recipe calories within limits
- forbidden ingredients absent

Simple forbidden ingredient list:

```text
chicken
beef
pork
fish
```

---

## Grocery Evals

Checks:

- no duplicate grocery items
- all recipe ingredients represented
- quantities aggregated correctly

---

## End-to-End Evals

Checks:

- overall dietary compliance
- overall calorie compliance
- grocery completeness

---

## Deliverables

- Eval framework
- Stage eval scores
- Eval summaries printed to console

---

# Phase 3 — Observability & Debugging

## Goal

Make failures diagnosable.

This phase directly targets interview prep goals.

---

## Add Pipeline Traces

Every run should record:

- stage inputs
- prompts
- raw outputs
- parsed outputs
- eval scores
- retry counts
- latency

Example:

```text
Run #42

Stage: MealPlanner
Eval Score: 0.92

Stage: RecipeGenerator
Eval Score: 0.41
Issue: Non-vegetarian ingredient detected
```

---

## Add Input Substitution

The system should support:

- bypassing stages
- injecting known-good outputs
- replaying previous stage outputs

Example usage:

```text
--use-known-good-recipes
```

Purpose:

- isolate failures
- validate downstream correctness
- support debugging

---

## Add Failure Injection

Purposely create failures:

- malformed JSON
- dietary violations
- missing ingredients
- inconsistent quantities

Then verify:

- evals catch failures
- pipeline traces identify failing stage

---

## Deliverables

- Run trace logging
- Failure injection system
- Stage isolation tools

---

## CLI Reference (Phase 3)

Run the app from the `App/` directory or via `dotnet run --project App`:

```sh
dotnet run --project App                                              # Normal end-to-end run
dotnet run --project App -- --use-known-good-meal-plan               # Bypass MealPlanner with hardcoded data
dotnet run --project App -- --use-known-good-recipes                 # Bypass RecipeGenerator with hardcoded data
dotnet run --project App -- --inject-meal-plan-failure=<mode>        # Inject a meal plan failure
dotnet run --project App -- --inject-recipe-failure=<mode>           # Inject a recipe failure
dotnet run --project App -- --help                                   # Print flag reference
```

Failure modes:

| Mode | What it injects |
|---|---|
| `malformed-json` | Unparseable JSON — triggers a parse failure and halts the stage |
| `dietary-violation` | Non-vegetarian output (e.g. chicken, beef) — evals should catch and flag |
| `missing-ingredients` | Empty ingredients list — RecipeEvaluator should flag |
| `exceed-calories` | Calories over the configured limit — evals should flag |

Flags can be combined. For example, to verify that evals catch dietary violations when the meal plan is bypassed with known-good data and only the recipe stage is broken:

```sh
dotnet run --project App -- --use-known-good-meal-plan --inject-recipe-failure=dietary-violation
```

Every run writes a JSON trace file to `runs/<run-id>.json` capturing stage inputs, prompts, raw LLM output, parsed output, eval scores, and injection flags.

---

# Phase 4 — Eval-Driven Retries

## Goal

Improve output quality automatically.

---

## Retry Flow

```text
Run stage
   ↓
Run eval
   ↓
If failed:
    generate feedback
    retry stage
```

---

## Example Feedback Prompt

```text
The recipe violates vegetarian constraints because it contains chicken.
Regenerate the recipe while remaining vegetarian.
```

---

## Retry Policies

Add:

- max retry count
- retry score improvements
- diminishing returns tracking

---

## Deliverables

- Automated retries
- Eval feedback loops
- Retry metrics

---

# Phase 5 — LLM Judge Evals

## Goal

Add semantic evaluation using another LLM.

---

## Example Judge Prompt

```text
Evaluate whether this recipe satisfies vegetarian dietary constraints.
Return:
- pass/fail
- confidence score
- explanation
```

---

## Compare Eval Types

Compare:

- deterministic evals
- LLM judge evals

Analyze:

- disagreements
- false positives
- false negatives

---

## Important Discussion Topic

LLM judges are themselves imperfect.

The project should explicitly acknowledge:

- evaluator reliability problems
- evaluator drift
- evaluator inconsistency

This is an important interview discussion area.

---

# Phase 6 — Optional UI / Visualization

## Goal

Improve inspectability.

Optional only.

---

## Ideas

- Web dashboard
- Pipeline visualization
- Eval score charts
- Retry history
- Prompt inspection
- Stage diff viewer

---

# 7. Proposed Solution Structure

```text
MealPlannerPipeline/

  Core/
    Contracts/
    Pipeline/
    Evals/
    Models/

  Stages/
    MealPlannerStage/
    RecipeGeneratorStage/
    GroceryAggregatorStage/
    NutritionStage/

  Infrastructure/
    Ollama/
    OpenAI/
    Logging/
    Serialization/

  Tests/

  App/
```

---

# 8. Important Interfaces

## Pipeline Stage

```csharp
public interface IPipelineStage<TInput, TOutput>
{
    Task<StageResult<TOutput>> ExecuteAsync(TInput input);
}
```

---

## Eval Interface

```csharp
public interface IEvaluator<T>
{
    Task<EvalResult> EvaluateAsync(T value);
}
```

---

## LLM Interface

```csharp
public interface ILlmClient
{
    Task<string> GenerateAsync(string prompt);
}
```

---

# 9. Important Interview Talking Points

This project should prepare discussion around:

- Why structured outputs matter
- Deterministic vs semantic evals
- Debugging multi-stage AI systems
- Root cause isolation
- Ground truth injection
- Observability for AI pipelines
- Reliability engineering for AI systems
- Retry policies
- Tradeoffs between model quality and speed
- Failure propagation
- Schema validation
- Why end-to-end metrics alone are insufficient

---

# 10. Explicit Interview Narrative

The intended interview narrative is:

> I built a multi-stage LLM meal planning pipeline where each stage produced structured outputs with independent evals. Instead of relying solely on end-to-end quality, I added deterministic validation, semantic evaluation, pipeline tracing, and stage isolation tools. When failures occurred, I could inject known-good outputs into downstream stages to identify root causes with confidence. I also experimented with eval-driven retries and compared deterministic evaluators against LLM judge evaluators.

This is the primary learning objective of the project.

---

# 11. Future Extensions

Potential future expansions:

- Cost estimation
- Multi-model comparisons
- Parallel stage execution
- Embedding-based similarity evals
- User preference learning
- Meal variety optimization
- Real grocery price APIs
- Reinforcement loop from user feedback
- Personalized recommendations
- Agentic planning systems

These are optional and should NOT block core eval infrastructure work.

---

# 12. Final Guidance

The most important thing about this project is NOT the meal planning domain.

The important thing is learning to think about AI systems as:

- pipelines
- contracts
- observability systems
- debugging systems
- evaluation systems

The key engineering insight is:

> Every stage should be independently testable and independently diagnosable.

That is the central lesson this project is intended to teach.

