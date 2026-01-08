namespace FMSoftlab.Validation.UnitTests
{
    public class Person
    {
        public string? Name { get; set; }
        public string? Email { get; set; }
        public int Age { get; set; }
        public bool IsActive { get; set; }
        public bool IsExternal { get; set; }
    }

    public class Order
    {
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public bool IsDraft { get; set; }
        public bool IsActive { get; set; }
    }


    // Usage example:

    public class ContactInfo
    {
        public string? Email { get; set; }
        public string? Phone { get; set; }
        public string? Mobile { get; set; }
    }

    public class ContactInfo2
    {
        public bool? HasMail { get; set; }
        public bool? HasPhone { get; set; }
        public bool? HasMobile { get; set; }
        public ContactInfo2()
        {
            HasMail = null;
            HasPhone = null;
            HasMobile = null;
        }
    }


    // Better approach - add a method to Validator<T>:
    public class ContactInfoValidator : Validator<ContactInfo>
    {
        public ContactInfoValidator()
        {
            AtLeastOneOf()
                .Property(x => x.Email)
                .Property(x => x.Phone)
                .Property(x => x.Mobile)
                .WithMessage("Please provide at least one contact method");
        }
    }


    public class UnitTest1
    {
        [Fact]
        public async Task AtLeastOnePropertyHasValue()
        {
            ContactInfoValidator val = new ContactInfoValidator();
            var info = new ContactInfo
            {
                Email = null,
                Phone = null,
                Mobile = null
            };
            var result = await val.ValidateAsync(info);
            Assert.False(result.IsValid);
            Console.WriteLine(result.Errors[0].Message);
            var info2 = new ContactInfo
            {
                Email = "fotismylonas@nowhere.com",
                Phone = null,
                Mobile = null
            };
            var result2 = await val.ValidateAsync(info2);
            Assert.True(result2.IsValid);
        }

        [Fact]
        public async Task AtLeastOneBooleanProperty_AllNulls_Invalid()
        {
            var validator = new Validator<ContactInfo2>();
            validator.AtLeastOneOf()
                     .Property(x => x.HasMail)
                     .Property(x => x.HasMobile)
                     .Property(x => x.HasPhone)
                     .WithMessage("Please provide at least one contact method");
            var info = new ContactInfo2();
            var result = await validator.ValidateAsync(info);
            Assert.False(result.IsValid);
            Console.WriteLine(result.Errors[0].Message);
        }

        [Fact]
        public async Task AtLeastOneBooleanProperty_Valid()
        {
            var validator = new Validator<ContactInfo2>();
            validator.AtLeastOneOf()
                     .Property(x => x.HasMail)
                     .Property(x => x.HasMobile)
                     .Property(x => x.HasPhone)
                     .WithMessage("Please provide at least one contact method");
            var info = new ContactInfo2
            {
                HasMail = true
            };
            var result = await validator.ValidateAsync(info);
            Assert.True(result.IsValid);
        }


        [Fact]
        public void RequiredRule_WithoutWhen_IsAlwaysExecuted()
        {
            var validator = new Validator<Person>();

            validator.RuleFor(x => x.Name)
                     .IsRequired();

            var person = new Person { Name = null };

            var result = validator.Validate(person);

            Assert.False(result.IsValid);
            Assert.Single(result.Errors);
            Assert.Equal("Name", result.Errors[0].PropertyName);
        }

        [Fact]
        public void Rule_WithWhenCondition_ExecutesOnlyWhenConditionIsTrue()
        {
            var validator = new Validator<Person>();

            validator.RuleFor(x => x.Email)
                     .IsEmail()
                     .When(p => p.IsActive);

            var inactivePerson = new Person
            {
                Email = "not-an-email",
                IsActive = false
            };

            var activePerson = new Person
            {
                Email = "not-an-email",
                IsActive = true
            };

            var inactiveResult = validator.Validate(inactivePerson);
            var activeResult = validator.Validate(activePerson);

            Assert.True(inactiveResult.IsValid);      // rule skipped
            Assert.False(activeResult.IsValid);       // rule executed
        }

        [Fact]
        public void Rule_WhenConditionTrue_ProducesValidationError()
        {
            var validator = new Validator<Person>();

            validator.RuleFor(x => x.Age)
                     .PositiveInt()
                     .When(p => p.IsActive);

            var person = new Person
            {
                Age = -5,
                IsActive = true
            };

            var result = validator.Validate(person);

            Assert.Single(result.Errors);
            Assert.Contains("positive", result.Errors[0].Message);
        }
        [Fact]
        public void Rule_WithUnlessCondition_IsSkippedWhenConditionIsTrue()
        {
            var validator = new Validator<Person>();

            validator.RuleFor(x => x.Name)
                     .IsRequired()
                     .Unless(p => p.IsExternal);

            var externalUser = new Person
            {
                Name = null,
                IsExternal = true
            };

            var internalUser = new Person
            {
                Name = null,
                IsExternal = false
            };

            var externalResult = validator.Validate(externalUser);
            var internalResult = validator.Validate(internalUser);

            Assert.True(externalResult.IsValid);   // skipped
            Assert.False(internalResult.IsValid);  // executed
        }

        [Fact]
        public async Task AsyncRule_WithWhenCondition_IsConditionallyExecuted()
        {
            var validator = new Validator<Person>();

            validator.RuleFor(x => x.Email)
                     .MustAsync(async (p, value) =>
                     {
                         await Task.Delay(10);
                         return value?.ToString() == "allowed@test.com";
                     })
                     .When(p => p.IsActive);

            var inactive = new Person
            {
                Email = "bad@test.com",
                IsActive = false
            };

            var active = new Person
            {
                Email = "bad@test.com",
                IsActive = true
            };

            var inactiveResult = await validator.ValidateAsync(inactive);
            var activeResult = await validator.ValidateAsync(active);

            Assert.True(inactiveResult.IsValid);
            Assert.False(activeResult.IsValid);
        }

        [Fact]
        public void MultipleRules_EachRespectsItsOwnWhenCondition()
        {
            var validator = new Validator<Person>();

            validator.RuleFor(x => x.Name)
                     .IsRequired()
                     .When(p => p.IsActive);

            validator.RuleFor(x => x.Email)
                     .IsEmail()
                     .When(p => !p.IsExternal);

            var person = new Person
            {
                Name = null,
                Email = "invalid",
                IsActive = false,
                IsExternal = true
            };

            var result = validator.Validate(person);

            Assert.True(result.IsValid); // both rules skipped
        }



        [Fact]
        public void ModelRule_Must_CreatesError_WhenPredicateFails()
        {
            var validator = new Validator<Order>();

            validator.Rule()
                .Must(o => o.StartDate <= o.EndDate)
                .WithMessage("StartDate must be before EndDate");

            var order = new Order
            {
                StartDate = DateTime.Today.AddDays(1),
                EndDate = DateTime.Today
            };

            var result = validator.Validate(order);

            Assert.False(result.IsValid);
            Assert.Single(result.Errors);
            Assert.Equal("StartDate must be before EndDate", result.Errors[0].Message);
        }

        [Fact]
        public void ModelRule_WithWhenCondition_IsSkipped_WhenConditionIsFalse()
        {
            var validator = new Validator<Order>();

            validator.Rule()
                .Must(o => o.StartDate <= o.EndDate)
                .When(o => !o.IsDraft)
                .WithMessage("Invalid date range");

            var draftOrder = new Order
            {
                StartDate = DateTime.Today.AddDays(5),
                EndDate = DateTime.Today,
                IsDraft = true
            };

            var result = validator.Validate(draftOrder);

            Assert.True(result.IsValid);
            Assert.Empty(result.Messages);
        }

        [Fact]
        public async Task AsyncModelRule_RespectsWhenCondition()
        {
            var validator = new Validator<Order>();

            validator.Rule()
                .MustAsync(async o =>
                {
                    await Task.Delay(10);
                    return o.EndDate >= DateTime.Today;
                })
                .When(o => o.IsActive)
                .WithMessage("EndDate cannot be in the past");

            var inactiveOrder = new Order
            {
                EndDate = DateTime.Today.AddDays(-1),
                IsActive = false
            };

            var activeOrder = new Order
            {
                EndDate = DateTime.Today.AddDays(-1),
                IsActive = true
            };

            var inactiveResult = await validator.ValidateAsync(inactiveOrder);
            var activeResult = await validator.ValidateAsync(activeOrder);

            Assert.True(inactiveResult.IsValid);     // skipped
            Assert.False(activeResult.IsValid);      // executed
            Assert.Single(activeResult.Errors);
        }

    }
}