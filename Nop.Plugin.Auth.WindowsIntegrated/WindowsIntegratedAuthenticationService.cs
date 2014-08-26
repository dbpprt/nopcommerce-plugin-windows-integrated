using System;
using System.Collections.Generic;
using System.DirectoryServices.AccountManagement;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.Principal;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using System.Web.Hosting;
using System.Web.Security;
using Autofac.Core.Activators.Reflection;
using Nop.Core;
using Nop.Core.Domain.Customers;
using Nop.Plugin.Auth.WindowsIntegrated.Properties;
using Nop.Services.Authentication;
using Nop.Services.Customers;

namespace Nop.Plugin.Auth.WindowsIntegrated
{
    public class WindowsIntegratedAuthenticationService : FormsAuthenticationService
    {
        private readonly HttpContextBase _httpContext;
        private readonly ICustomerService _customerService;

        public WindowsIntegratedAuthenticationService(
            HttpContextBase httpContext,
            ICustomerService customerService,
            CustomerSettings customerSettings)
            : base(httpContext, customerService, customerSettings)
        {
            _httpContext = httpContext;
            _customerService = customerService;
        }


        private Customer EnsureUser()
        {
            var encryptedCookie = _httpContext.Request.Cookies[FormsAuthentication.FormsCookieName];
            if (encryptedCookie != null && !string.IsNullOrEmpty(encryptedCookie.Value))
            {
                var ticket = FormsAuthentication.Decrypt(encryptedCookie.Value);
                var result = GetAuthenticatedCustomerFromTicket(ticket);
                if (result != null && result.Active && !result.Deleted && result.IsRegistered())
                {
                    return result;
                }
            }

            Customer customer;

            var identity = _httpContext.User.Identity;
            if (identity == null)
            {
                return null;
            }

            var identityName = identity.Name;
            using (HostingEnvironment.Impersonate())
            {
                using (var context = new PrincipalContext(ContextType.Domain))
                using (var principal = UserPrincipal.FindByIdentity(context, IdentityType.SamAccountName, identityName))
                {
                    var existing = _customerService.GetCustomerByEmail(principal.EmailAddress);

                    if (existing != null && existing.Active && !existing.Deleted && existing.IsRegistered())
                    {
                        MapRoles(existing, _httpContext.User);

                        SignIn(existing, true);
                        return existing;
                    }

                    customer = new Customer
                    {
                        Active = true,
                        Email = principal.EmailAddress,
                        HasShoppingCartItems = false,
                        IsSystemAccount = false,
                        Username = principal.EmailAddress
                    };

                    MapRoles(customer, _httpContext.User);
                }
            }

            var password = GetRandomString(25);

            var registeredRole = _customerService.GetCustomerRoleBySystemName(SystemCustomerRoleNames.Registered);
            if (registeredRole == null)
                throw new NopException("'Registered' role could not be loaded");
            customer.CustomerRoles.Add(registeredRole);

            var guestRole = customer.CustomerRoles.FirstOrDefault(cr => cr.SystemName == SystemCustomerRoleNames.Guests);
            if (guestRole != null)
                customer.CustomerRoles.Remove(guestRole);
            customer.Password = password;
            customer.PasswordFormat = PasswordFormat.Clear;
            customer.CreatedOnUtc = DateTime.UtcNow;
            customer.LastLoginDateUtc = DateTime.UtcNow;
            customer.LastActivityDateUtc = DateTime.UtcNow;

            _customerService.InsertCustomer(customer);

            return customer;
        }

        private void MapRoles(Customer customer, IPrincipal user)
        {
            var mappings = Settings.Default.GroupMappings.Split(';');

            foreach (var mapping in mappings)
            {
                var arr = mapping.Split(':');

                var from = arr.First();
                var to = arr.Last();

                if (user.IsInRole(from))
                {
                    var role = _customerService.GetCustomerRoleBySystemName(to);

                    if (role == null)
                    {
                        _customerService.InsertCustomerRole(new CustomerRole
                        {
                            Active = true,
                            FreeShipping = false,
                            IsSystemRole = false,
                            Name = to,
                            SystemName = to,
                            TaxExempt = false
                        });
                    }

                    customer.CustomerRoles.Add(_customerService.GetCustomerRoleBySystemName(to));
                }
            }
        }

        public override Customer GetAuthenticatedCustomer()
        {
            var customer = EnsureUser();
            return customer;
        }

        #region Helpers
        // http://blog.codeeffects.com/Article/Generate-Random-Numbers-And-Strings-C-Sharp

        private string GetRandomString(int length)
        {
            var array = new string[54]
	        {
		        "0","2","3","4","5","6","8","9",
		        "a","b","c","d","e","f","g","h","j","k","m","n","p","q","r","s","t","u","v","w","x","y","z",
		        "A","B","C","D","E","F","G","H","J","K","L","M","N","P","R","S","T","U","V","W","X","Y","Z"
	        };
            var sb = new StringBuilder();
            for (int i = 0; i < length; i++) sb.Append(array[GetRandomNumber(53)]);
            return sb.ToString();
        }

        private int GetRandomNumber(int maxNumber)
        {
            if (maxNumber < 1)
                throw new Exception("The maxNumber value should be greater than 1");
            var b = new byte[4];
            new System.Security.Cryptography.RNGCryptoServiceProvider().GetBytes(b);
            var seed = (b[0] & 0x7f) << 24 | b[1] << 16 | b[2] << 8 | b[3];
            var r = new Random(seed);
            return r.Next(1, maxNumber);
        }

        #endregion
    }
}
