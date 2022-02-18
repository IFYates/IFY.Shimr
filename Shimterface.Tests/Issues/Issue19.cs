using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;

namespace Shimterface.Tests.Issues
{
    /// <summary>
    /// https://github.com/IFYates/Shimterface/issues/19
    /// </summary>
    [TestClass]
    public class Issue19
    {
        private const string KEY = @"<RSAKeyValue><Modulus>yq1HPYW/Y1EBcwCGVkBm9tFX4Z17fKul4H8rZTTF4M/fk8S1CzVZL5/cVMJEcZnYkYc/ZFiM3Hpd1SOun6s8BZFM3ZhqaHk31nH29n9KolzpvQsjo5jwcYCtILC0WanLk+W3uK51fux0+mO35Qs50BBzZP18sDBVcW1/Iy7mO9oSM8eKfVGvfd1EyjF9AAOBDzPiVgJKv1BYzzRlwI2zpDPmJEYbwiPRyBaoxHrDm9b9hsAlvyjG/9lL8y+32035dbnYqQdZomri+UVFxxa1pmaKI86ro2aifRAlzostRzh63d8w0T2YTU3FqA8V6NM0cwTva3R1a/xMWlKtG8PD9Q==</Modulus><Exponent>AQAB</Exponent><P>1MR2rZQxZXf2soBQOXHMDcH2wOp0tasHgvGeNT8eA54DnpfVjXPOABGOWfbFTeEvirj88nI7wvYaiBrVkk1EYHHIt8rwxZwWqJeX8uRL8WyQx+UB0UWmFXCrXAX9a2ig48V9DRrv5pWU8cxgK3JfQ1TBG0StU0HXDXOBgyybypM=</P><Q>89vu266wBIHvnWGGCqoes5+yl0Eq4Pc5IzHSIYfvQzbi6t4NdyTLH1G7Q7GUSDhQ7fDosojn1trs7nZMtrVnzi51bafaT6+NClPE9KhxD+vy4KMtpSJQZa2uI8ZGtf/xZ6FKCbohTFE4etTW7It/M+W/h1F/1ohp3gY+Fdld5Fc=</Q><DP>iENajkg+anGt+RvcRS3wNU9nrJ17KW3jXaVIYWmO21ozjzpGdlUYNUsJE+zK59m0DO/0b4FhbLcYvtoQtaXuiKXWmn7KPIR+rnKHyfMUAAY7owmzZEdq2ohR9pmPd16Gy9kLAX2i7tYVkdYGMU0KXaDGG6ScLJoaFG9JHq1PCSk=</DP><DQ>SpcFMrD2KgrMjikcZPqwNWUtrVJDmVhFY9yDV7pKlxacxhZxq/XXI5dOXmBc6NJA/ubnZmS19WQ1gKMyx9gpDknrpUToY+Ngkr4YynUTUDltqwR+m7opOVCsqUimrFjDMF2HVf5W3Q8i5X9g1i29FNS7htqI7cgACeeC6g/4xjU=</DQ><InverseQ>B8Az3R3zh3cVCnbiK3q+DOHMjE2HYzAnr9epxXcX8V0oeZDfmhRyiM4qRfCs7xmhv1TWT2g+c4MEoHZ5T58ifHL25UV+v+zdsbNw1FDc658e0yvO2Z1dvC1WKMGV91/yyEIJ+741cKEX5GneckwgPqJm5HeC+YxUgS+yyNugrws=</InverseQ><D>tfAs5nZ84xvhsCnFbgHsLB6qxDaJltXDVy7xdq2UAIa6jjzjxIcEY7Mep9uoc04G0kTVzC+Na5JRTzbz2BNwExNnK/lZdCV00YWGi6qjBfNgQ7qPKJnvJgS75X+atm6s+Dwb26aIhQKg0/DWML8OC8/otryyxLruyJ7hpWTBevFiwvIUZjBIxvjWywqwnFn3H0MhWoE2bGweGQEr7B+0DbfFvinnrfv6vnAuS/cgkkM2i8d+FfkLoV7uLVIfskoy+cdMjiA1vKycX6+7fqfuKojDu96Ee7HnNafCiILcjeci7sbs9Kmp8QTAZltYbiV7AxQIoOCabTn28kI1ooXAtQ==</D></RSAKeyValue>";

        public interface IRSA_Issue : IDisposable
        {
            void FromXmlString(string xmlString);
            byte[] Encrypt(byte[] data, System.Security.Cryptography.RSAEncryptionPadding padding);
        }

        [TestMethod]
        public void Issue19__Cannot_shim_some_RSA_methods()
        {
            var data = BitConverter.GetBytes(DateTime.UtcNow.Ticks);
            var obj = System.Security.Cryptography.RSA.Create();
            var shim = obj.Shim<IRSA_Issue>();
            shim.FromXmlString(KEY); // Works as expected

            Assert.ThrowsException<MethodAccessException>(() =>
            {
                _ = shim.Encrypt(data, System.Security.Cryptography.RSAEncryptionPadding.OaepSHA1); // Fails here
            });
        }

        public interface IRSA_Workaround : IDisposable
        {
            void FromXmlString(string xmlString);
            [Shim(typeof(System.Security.Cryptography.RSA))]
            byte[] Encrypt(byte[] data, System.Security.Cryptography.RSAEncryptionPadding padding);
        }

        [TestMethod]
        public void Issue19__Workaround()
        {
            var data = BitConverter.GetBytes(DateTime.UtcNow.Ticks);
            var obj = System.Security.Cryptography.RSA.Create();
            var shim = obj.Shim<IRSA_Workaround>();
            shim.FromXmlString(KEY); // Works as expected
            var output = shim.Encrypt(data, System.Security.Cryptography.RSAEncryptionPadding.OaepSHA1); // Fails here
            Assert.IsNotNull(output);
        }
    }
}
