using System;
using System.Net;
using System.Text.Json;
using NUnit.Framework;
using RestSharp;
using RestSharp.Authenticators;
using ExamStorySpoil.Models;


namespace SpoilStoryExam
{
    [TestFixture]
    public class SpoilStoryApiTests
    {
        private RestClient client;
        private static string lastCreatedStoryId;

        private const string BaseUrl = "https://d3s5nxhwblsjbi.cloudfront.net/";

        private const string StaticToken = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJzdWIiOiJKd3RTZXJ2aWNlQWNjZXNzVG9rZW4iLCJqdGkiOiIzZDM0NTIyNy0zODI5LTRhMWQtOTJhNS1iMDQ2MjIwNzFhMmQiLCJpYXQiOiIwOC8xNi8yMDI1IDA2OjI2OjI5IiwiVXNlcklkIjoiZTgyNjVlZmQtOGFmYi00ZWI1LThkZmQtMDhkZGRiMWExM2YzIiwiRW1haWwiOiJhcGlfdGVzdEBhYnYuYmciLCJVc2VyTmFtZSI6ImV4YW1fdGVzdCIsImV4cCI6MTc1NTM0NzE4OSwiaXNzIjoiU3RvcnlTcG9pbF9BcHBfU29mdFVuaSIsImF1ZCI6IlN0b3J5U3BvaWxfV2ViQVBJX1NvZnRVbmkifQ.ByZsiHR-QGAS8OJ4efyThn8zvwmBiZyF-KjqP1_CH-Q";

        private const string LoginEmail = "exam_test";
        private const string LoginPassword = "123456";

        [OneTimeSetUp]
        public void Setup()
        {
            string jwtToken;

            if (!string.IsNullOrWhiteSpace(StaticToken))
            {
                jwtToken = StaticToken;
            }
            else
            {
                jwtToken = GetJwtToken(LoginEmail, LoginPassword);
            }

            var options = new RestClientOptions(BaseUrl)
            {
                Authenticator = new JwtAuthenticator(jwtToken),
            };

            this.client = new RestClient(options);
        }

        private string GetJwtToken(string email, string password)
        {
            var tempCLient = new RestClient(BaseUrl);
            var request = new RestRequest("/api/User/Authentication", Method.Post);
            request.AddJsonBody(new { email, password });

            var response = tempCLient.Execute(request);

            if (response.StatusCode == HttpStatusCode.OK)
            {
                var content = JsonSerializer.Deserialize<JsonElement>(response.Content);
                var token = content.GetProperty("accessToken").GetString();

                if (string.IsNullOrWhiteSpace(token))
                {
                    throw new InvalidOperationException("Failed to retrieve JWT token from the response.");
                }
                return token;
            }
            else
            {
                throw new InvalidOperationException($"Failed to authenticate. Status code: {response.StatusCode}, Content: {response.Content}");
            }
        }
        //All test here

       
        [Test, Order(1)]
        public void Test_CreateStorySpoiler()
        {
            var story = new StoryDTO
            {
                Title = "Test Story",
                Description = "This is a spoiler test.",
                Url = ""
            };

            var request = new RestRequest("/api/Story/Create", Method.Post);
            request.AddJsonBody(story);

            var response = client.Execute<ApiResponseDTO>(request);

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Created));
            Assert.That(response.Data.StoryId, Is.Not.Null.Or.Empty);
            Assert.That(response.Data.Msg, Is.EqualTo("Successfully created!"));

            lastCreatedStoryId = response.Data.StoryId;
        }

        [Test, Order(2)]
        public void Test_EditStorySpoiler()
        {
            var updatedStory = new StoryDTO
            {
                Title = "New Test Story",
                Description = "New spoiler description.",
                Url = ""
            };

            var request = new RestRequest($"/api/Story/Edit/{lastCreatedStoryId}", Method.Put);
            request.AddJsonBody(updatedStory);

            var response = client.Execute<ApiResponseDTO>(request);

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            Assert.That(response.Data.Msg, Is.EqualTo("Successfully edited"));
        }

        [Test, Order(3)]
        public void Test_GetAllStories()
        {
            var request = new RestRequest("/api/Story/All", Method.Get);
            var response = client.Execute(request);

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            Assert.That(response.Content, Does.Contain("title"));
        }

        [Test, Order(4)]
        public void Test_DeleteStorySpoiler()
        {
            var request = new RestRequest($"/api/Story/Delete/{lastCreatedStoryId}", Method.Delete);
            var response = client.Execute<ApiResponseDTO>(request);

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            Assert.That(response.Data.Msg, Is.EqualTo("Deleted successfully!"));
        }

        [Test, Order(5)]
        public void Test_CreateStoryWithoutRequiredFields()
        {
            var story = new StoryDTO
            {
                Title = "",
                Description = "",
                Url = ""
            };

            var request = new RestRequest("/api/Story/Create", Method.Post);
            request.AddJsonBody(story);

            var response = client.Execute(request);

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
        }

        [Test, Order(6)]
        public void Test_EditNonExistingStory()
        {
            var story = new StoryDTO
            {
                Title = "Non existing",
                Description = "Should fail",
                Url = ""
            };

            var request = new RestRequest("/api/Story/Edit/invalid-id-124563", Method.Put);
            request.AddJsonBody(story);

            var response = client.Execute<ApiResponseDTO>(request);

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
            Assert.That(response.Data.Msg, Is.EqualTo("No spoilers..."));
        }

        [Test, Order(7)]
        public void Test_DeleteNonExistingStory()
        {
            var request = new RestRequest("/api/Story/Delete/invalid-id-123", Method.Delete);
            var response = client.Execute<ApiResponseDTO>(request);

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
            Assert.That(response.Data.Msg, Is.EqualTo("Unable to delete this story spoiler!"));
        }

        [OneTimeTearDown]
        public void Teardown()
        {
            this.client?.Dispose();
        }


    }
}