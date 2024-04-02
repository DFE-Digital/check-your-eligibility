using CheckYourEligibility.SystemTests.Utilities.Models;
using Newtonsoft.Json;
using NUnit.Framework;
using System;
using System.Net;
using System.Threading.Tasks;

namespace CheckYourEligibility.SystemTests.API
{
    internal class SchoolSearchStatus
    {
        [Test]
        [Ignore("Targeting live environment")]
        public async Task GetRequestSchoolSearchWithValidName()
        {
            var endpoint = "/Schools/search?query=hinde house";

            var response = await ApiHelper.GetRequest(endpoint);
            var jsonResponse = await response.Content.ReadAsStringAsync();
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));

            // Deserialize the response into a model
            var deserializedResponse = JsonConvert.DeserializeObject<SchoolSearchStatusModel>(jsonResponse);

            Assert.IsNotEmpty(deserializedResponse.Data); // Check that we got some data back


            var firstSchool = deserializedResponse.Data[0];
            Assert.That(firstSchool.name, Is.EqualTo("Hinde House 2-16 Academy"));

            Console.WriteLine("School Name:" + firstSchool.name);
        }

        [Test]
        [Ignore("Targeting live environment")]
        public async Task GetRequestSchoolSearchWithInvalidName()
        {
            var endpoint = "/Schools/search?query=hinde house123";

            var response = await ApiHelper.GetRequest(endpoint);
            var jsonResponse = await response.Content.ReadAsStringAsync();
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));

            var deserializedResponse = JsonConvert.DeserializeObject<SchoolSearchStatusModel>(jsonResponse);
            Assert.IsEmpty(deserializedResponse.Data);

        }

        [Test]
        [Ignore("Targeting live environment")]
        public async Task GetRequestSchoolSearchWith20Records()
        {
            var endpoint = "/Schools/search?query=hin";

            var response = await ApiHelper.GetRequest(endpoint);
            var jsonResponse = await response.Content.ReadAsStringAsync();
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));

            var deserializedResponse = JsonConvert.DeserializeObject<SchoolSearchStatusModel>(jsonResponse);
            Assert.IsNotEmpty(deserializedResponse.Data);
            Assert.That(deserializedResponse.Data.Count, Is.AtMost(20), "The data list should contain a maximum of 20 records.");

        }

        [Test]
        [Ignore("Targeting live environment")]
        public async Task GetRequestSchoolSearchWitNoName()
        {
            var endpoint = "/Schools/search?query=";

            var response = await ApiHelper.GetRequest(endpoint);
            var jsonResponse = await response.Content.ReadAsStringAsync();
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));

        }

        [Test]
        [Ignore("Targeting live environment")]
        public async Task GetRequestSchoolSearchCheckResponseBody()
        {
            var endpoint = "/Schools/search?query=hinde house";

            var response = await ApiHelper.GetRequest(endpoint);
            var jsonResponse = await response.Content.ReadAsStringAsync();
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));

            var deserializedResponse = JsonConvert.DeserializeObject<SchoolSearchStatusModel>(jsonResponse);
            Assert.IsNotEmpty(deserializedResponse.Data);
            var firstSchool = deserializedResponse.Data[0];
            Assert.That(firstSchool.name, Is.EqualTo("Hinde House 2-16 Academy"));
            Assert.That(firstSchool.id, Is.EqualTo(139856));
            Assert.That(firstSchool.postcode, Is.EqualTo("S5 6AG"));
            Assert.That(firstSchool.street, Is.EqualTo("Shiregreen Lane"));
            Assert.That(firstSchool.town, Is.EqualTo("Sheffield"));
            Assert.That(firstSchool.county, Is.EqualTo("South Yorkshire"));
            Assert.That(firstSchool.la, Is.EqualTo("Sheffield"));
            Assert.That(firstSchool.distance, Is.EqualTo(0.5416666666666666));

        }
    }
}
