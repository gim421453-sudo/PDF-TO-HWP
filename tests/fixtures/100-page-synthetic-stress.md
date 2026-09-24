# 100-page synthetic stress fixture

The automated stress baseline uses an in-memory, copyright-free logical PDF
fixture with pages `PAGE 001` through `PAGE 100`. It exercises workspace
identity, ordering, bulk exclusion, snapshot creation and cancellation without
requiring a Hancom layout validation. The fixture is deliberately generated
inside tests so no binary PDF is checked into the repository.
