flowchart LR
  client["Client<br/>Swagger / Postman"] --> api["BankTransfer.Api<br/>Controllers"]

  api -->|calls via interfaces| app["BankTransfer.Application<br/>Services (Use Cases)<br/>Ej: TransferFundsService"]
  app --> domain["BankTransfer.Domain<br/>Entities + Rules + Exceptions"]

  app -->|depends on abstractions| abs["BankTransfer.Application.Abstractions<br/>Interfaces<br/>IAccountRepository<br/>ITransferRepository<br/>IIdempotencyStore<br/>IUnitOfWork"]

  app -->|DI| infra["BankTransfer.Infrastructure<br/>Implementations<br/>AccountRepository<br/>TransferRepository<br/>IdempotencyStore<br/>UnitOfWork<br/>EF Core DbContext"]
  infra --> db[("Database<br/>SQLite")]

  infra -->|implements| abs
