# resource-api-cqrs-reference

A learning-focused reference implementation of a **.NET Resource API** built using:

* CQRS (Command Query Responsibility Segregation)
* Specification-Driven Development (SDD)
* Vertical Slice Architecture
* Clean, testable application boundaries

This repository is not meant to be a production system.
Its purpose is to **explore architectural thinking**, not just build endpoints.

---

## 🎯 Goals

This project exists to answer questions like:

* How does CQRS actually feel in a real API?
* How do specifications replace ad-hoc filtering logic?
* Can requirements drive code structure instead of the database?
* How do we design features instead of CRUD endpoints?

The emphasis is learning *design*, not frameworks.

---

## 🧠 Key Concepts Practiced

### CQRS

Separate models for:

* **Commands** → change state
* **Queries** → read state

No shared logic between read and write flows.

### Specification-Driven Development

Business rules are expressed as composable specifications instead of:

* scattered LINQ
* fat repositories
* duplicated filters

Specifications become reusable domain knowledge.

### Vertical Slice Architecture

Features are grouped by behavior rather than technical layer.

Instead of:

```
Controllers/
Services/
Repositories/
Dtos/
```

We structure around:

```
Features/
    CreateResource/
    UpdateResource/
    GetResource/
    SearchResources/
```

---

## 🏗️ Architectural Intent

The system prioritizes:

* Explicit behavior over generic abstractions
* Small feature slices over large shared services
* Domain clarity over DRY at all costs
* Readability over cleverness

If something feels repetitive but clearer → repetition wins.

---

## 🧪 What This Project Is (and Isn’t)

### ✔ This project is

* A learning sandbox
* A reference for future projects
* A place to experiment with architecture safely
* A notebook of patterns that worked (and failed)

### ❌ This project is NOT

* A production template
* A generic reusable framework
* A demonstration of perfect practices

Tradeoffs are documented, not hidden.

---

## 🚀 Running the Project

```bash
# restore
dotnet restore

# run api
dotnet run --project src/Api
```

Once running, open:

```
https://localhost:<port>/swagger
```

---

## 🧭 Development Workflow

1. Start from a requirement
2. Write or define the specification
3. Create the query/command handler
4. Implement persistence
5. Add endpoint last

> The API is the delivery mechanism — not the design center.

---

## 📚 What I Expect To Learn

* When CQRS improves clarity — and when it doesn’t
* How specifications affect testability
* How vertical slices change feature ownership
* The cost of abstraction vs duplication
* How to design behavior instead of tables

---

## 🗺️ Planned Experiments

* Pagination & filtering via specifications
* Business rule validation strategies
* Transaction boundaries in CQRS
* Read model optimization
* Testing strategies (unit vs integration vs behavior)

---

## 📝 Notes

This repository will evolve frequently.
Breaking changes are expected — the history matters more than stability.

If something looks unusual, it was probably intentional to explore an idea.

---

## License

MIT
