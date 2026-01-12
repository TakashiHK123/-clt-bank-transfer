using BankTransfer.Domain.Entities;
using BankTransfer.Domain.Exceptions;

namespace BankTransfer.UnitTests.Domain.Entities;

public sealed class AccountTests
{
        [Fact]
        public void Ctor_CuandoUserIdEsVacio_DebeLanzarArgumentException()
        {
            var ex = Assert.Throws<ArgumentException>(() =>
                new Account(Guid.Empty, "Main", 0m, "PYG"));

            Assert.Equal("userId", ex.ParamName);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void Ctor_CuandoNameEsNuloOVacio_DebeLanzarArgumentException(string? name)
        {
            var ex = Assert.Throws<ArgumentException>(() =>
                new Account(Guid.NewGuid(), name!, 0m, "PYG"));

            Assert.Equal("name", ex.ParamName);
        }

        [Fact]
        public void Ctor_CuandoInitialBalanceEsNegativo_DebeLanzarArgumentOutOfRangeException()
        {
            var ex = Assert.Throws<ArgumentOutOfRangeException>(() =>
                new Account(Guid.NewGuid(), "Main", -0.01m, "PYG"));

            Assert.Equal("balance", ex.ParamName);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void Ctor_CuandoCurrencyEsNuloOVacio_DebeLanzarArgumentException(string? currency)
        {
            var ex = Assert.Throws<ArgumentException>(() =>
                new Account(Guid.NewGuid(), "Main", 0m, currency!));

            Assert.Equal("currency", ex.ParamName);
        }

        [Theory]
        [InlineData("PY")] 
        [InlineData("PYGG")]   
        [InlineData("12A")]       
        [InlineData("P$G")]       
        [InlineData("P G")]      
        public void Ctor_CuandoCurrencyNoTieneTresLetras_DebeLanzarArgumentException(string currency)
        {
            var ex = Assert.Throws<ArgumentException>(() =>
                new Account(Guid.NewGuid(), "Main", 0m, currency));

            Assert.Equal("currency", ex.ParamName);
        }

        [Fact]
        public void Ctor_DebeRecortarName_YPonerCurrencyEnMayusculas()
        {
            var userId = Guid.NewGuid();

            var acc = new Account(userId, "  Caja Principal  ", 100m, " pyg ");

            Assert.Equal(userId, acc.UserId);
            Assert.Equal("Caja Principal", acc.Name);
            Assert.Equal(100m, acc.Balance);
            Assert.Equal("PYG", acc.Currency);
            Assert.Equal(0L, acc.Version);
            Assert.NotEqual(Guid.Empty, acc.Id);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        public void Debit_CuandoAmountEsCeroONegativo_DebeLanzarArgumentOutOfRangeException(decimal amount)
        {
            var acc = new Account(Guid.NewGuid(), "Main", 100m, "PYG");

            var ex = Assert.Throws<ArgumentOutOfRangeException>(() => acc.Debit(amount));

            Assert.Equal("amount", ex.ParamName);
        }

        [Fact]
        public void Debit_CuandoNoHaySaldoSuficiente_DebeLanzarInsufficientFundsException_YNoCambiarEstado()
        {
            var acc = new Account(Guid.NewGuid(), "Main", 50m, "PYG");
            var balanceBefore = acc.Balance;
            var versionBefore = acc.Version;

            var ex = Assert.Throws<InsufficientFundsException>(() => acc.Debit(60m));

            Assert.Contains(acc.Id.ToString(), ex.Message);
            Assert.Equal(balanceBefore, acc.Balance);
            Assert.Equal(versionBefore, acc.Version);
        }

        [Fact]
        public void Debit_CuandoEsValido_DebeDisminuirBalance_EIncrementarVersion()
        {
            var acc = new Account(Guid.NewGuid(), "Main", 100m, "PYG");

            acc.Debit(40m);

            Assert.Equal(60m, acc.Balance);
            Assert.Equal(1L, acc.Version);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-10)]
        public void Credit_CuandoAmountEsCeroONegativo_DebeLanzarArgumentOutOfRangeException(decimal amount)
        {
            var acc = new Account(Guid.NewGuid(), "Main", 100m, "PYG");

            var ex = Assert.Throws<ArgumentOutOfRangeException>(() => acc.Credit(amount));

            Assert.Equal("amount", ex.ParamName);
        }

        [Fact]
        public void Credit_CuandoEsValido_DebeAumentarBalance_EIncrementarVersion()
        {
            var acc = new Account(Guid.NewGuid(), "Main", 100m, "PYG");

            acc.Credit(25m);

            Assert.Equal(125m, acc.Balance);
            Assert.Equal(1L, acc.Version);
        }

        [Fact]
        public void Credit_LuegoDebit_DebeIncrementarVersionEnCadaOperacion()
        {
            var acc = new Account(Guid.NewGuid(), "Main", 100m, "PYG");

            acc.Credit(10m);
            acc.Debit(5m);

            Assert.Equal(105m, acc.Balance);
            Assert.Equal(2L, acc.Version);
        }

        [Fact]
        public void Seed_CuandoIdEsVacio_DebeLanzarArgumentException()
        {
            var ex = Assert.Throws<ArgumentException>(() =>
                Account.Seed(Guid.Empty, Guid.NewGuid(), "Main", 0m, "PYG"));

            Assert.Equal("id", ex.ParamName);
        }

        [Fact]
        public void Seed_DebeAsignarIdProporcionado_YMantenerOtrasReglas()
        {
            var id = Guid.NewGuid();
            var userId = Guid.NewGuid();

            var acc = Account.Seed(id, userId, "  Main  ", 10m, "usd");

            Assert.Equal(id, acc.Id);
            Assert.Equal(userId, acc.UserId);
            Assert.Equal("Main", acc.Name);
            Assert.Equal(10m, acc.Balance);
            Assert.Equal("USD", acc.Currency);
            Assert.Equal(0L, acc.Version);
        }
}

