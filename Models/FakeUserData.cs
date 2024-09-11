using Bogus;
namespace MiniProject_GMD.Models
{
    public class FakeUserData
    {
        Faker<User> UserFakeModel;
        public FakeUserData()
        {
          Randomizer.Seed = new Random(123);
          UserFakeModel = new Faker<User>()
            .RuleFor(u => u.FirstName, f => f.Name.FirstName())
            .RuleFor(u => u.LastName, f => f.Name.LastName())
            .RuleFor(u => u.BirthDate, f => f.Date.Past(30, DateTime.Now.AddYears(-18)))
            .RuleFor(u => u.City, f => f.Address.City())
            .RuleFor(u => u.Country, f => f.Address.CountryCode(Bogus.DataSets.Iso3166Format.Alpha2))
            .RuleFor(u => u.Email, (f, u) => f.Internet.Email(u.FirstName, u.LastName))
            .RuleFor(u => u.Mobile, f => f.Phone.PhoneNumber())
            .RuleFor(u => u.Avatar, f => f.Internet.Avatar())
            .RuleFor(u => u.Company, f => f.Company.CompanyName())
            .RuleFor(u => u.JobPosition, f => f.Name.JobTitle())
            .RuleFor(u => u.Username, (f, u) => f.Internet.UserName(u.FirstName, u.LastName))
            .RuleFor(u => u.Password, f => f.Internet.Password(f.Random.Int(6, 10)))
            .RuleFor(u => u.Role, f => f.PickRandom<UserRoles>(UserRoles.user, UserRoles.admin));

        }
        public List<User> GeneratePersons(int count)
        {
            List<User> users = new List<User>(); 
            for (int i = 0; i < count; i++)
            {
                users.Add(UserFakeModel.Generate());
            }
            return users;
        }
    

}
}
