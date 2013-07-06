﻿using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web.Http;
using System.Web.Http.Hosting;
using AgrcPasswordManagement;
using AgrcPasswordManagement.Commands;
using AgrcPasswordManagement.Models.Account;
using ArcGisServerPermissionsProxy.Api.Controllers;
using ArcGisServerPermissionsProxy.Api.Formatters;
using ArcGisServerPermissionsProxy.Api.Models.Database;
using ArcGisServerPermissionsProxy.Api.Models.Response;
using ArcGisServerPermissionsProxy.Api.Tests.Infrastructure;
using CommandPattern;
using NUnit.Framework;

namespace ArcGisServerPermissionsProxy.Api.Tests
{
    [TestFixture]
    public class AuthenticateUserTests : RavenEmbeddableTest
    {
        private AuthenticateController _controller;
        private const string Pepper = ")(*&(*^%*&^$*^#$";

        public override void SetUp()
        {
            base.SetUp();

            var salt = CommandExecutor.ExecuteCommand(new GenerateSaltCommand());
            var password = CommandExecutor.ExecuteCommand(new HashPasswordCommand("123abc", salt, Pepper)).Result;

            var user = new User("test@test.com", password.HashedPassword, salt, "app1", "role1");

            using (var s = DocumentStore.OpenSession())
            {
                s.Store(user);
                s.SaveChanges();
            }

            var config = new HttpConfiguration();
            var request = new HttpRequestMessage(HttpMethod.Post, "http://localhost/");

            _controller = new AuthenticateController
            {
                Request = request,
                DocumentStore =  DocumentStore
            };

            _controller.Request.Properties[HttpPropertyKeys.HttpConfigurationKey] = config;
        }

        private static ResponseContainer<AuthenticationResponse> GetResultContent(HttpResponseMessage response)
        {
            //Debug.Print(response.Result.Content.ReadAsStringAsync().Result);

            return response.Content.ReadAsAsync<ResponseContainer<AuthenticationResponse>>(new[]
                    {
                        new TextPlainResponseFormatter()
                    }).Result;
        }

        [Test]
        public async Task UserCanAuthenticatewithCorrectPassword()
        {
            var login = new LoginCredentials("test@test.com", "123abc", "app1", "role1");

            var response = await _controller.Post(login);

            var result = GetResultContent(response);

            Assert.That(result.Status, Is.EqualTo((int)HttpStatusCode.OK));
            Assert.That(result.Result, Is.Not.Null);
            Assert.That(result.Result.Token, Is.Not.Null);
        }

        [Test]
        public async Task UserIsDeniedOnBadPassword()
        {
            var login = new LoginCredentials("test@test.com", "wrong", "app1", "role1");

            var response = await _controller.Post(login);

            var result = GetResultContent(response);

            Assert.That(result.Status, Is.EqualTo((int)HttpStatusCode.Unauthorized));
            Assert.That(result.Result, Is.Not.Null);
            Assert.That(result.Result.Token, Is.Null);
        }
    }
}