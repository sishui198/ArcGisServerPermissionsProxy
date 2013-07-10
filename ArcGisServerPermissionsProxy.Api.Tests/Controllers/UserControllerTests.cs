﻿using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web.Http;
using System.Web.Http.Hosting;
using ArcGisServerPermissionsProxy.Api.Controllers;
using ArcGisServerPermissionsProxy.Api.Models.Response;
using ArcGisServerPermissionsProxy.Api.Raven.Indexes;
using ArcGisServerPermissionsProxy.Api.Raven.Models;
using ArcGisServerPermissionsProxy.Api.Tests.Infrastructure;
using NUnit.Framework;

namespace ArcGisServerPermissionsProxy.Api.Tests.Controllers
{
    public class UserControllerTests : RavenEmbeddableTest
    {
        private const string Database = "";
        private UserController _controller;

        public override void SetUp()
        {
            base.SetUp();

            var notApprovedActiveUser = new User("notApprovedActiveUser@test.com", "password", "", "",
                                                 new Collection<string>());

            var approvedActiveUser = new User("approvedActiveUser@test.com", "password", "", "",
                                              new Collection<string> {"admin", "boss"})
                {
                    Active = false,
                    Approved = true
                };

            var notApprovedNotActiveUser = new User("notApprovedNotActiveUser@test.com", "password", "", "",
                                                    new Collection<string>())
                {
                    Active = false
                };

            using (var s = DocumentStore.OpenSession())
            {
                s.Store(approvedActiveUser);
                s.Store(notApprovedActiveUser);
                s.Store(notApprovedNotActiveUser);

                s.SaveChanges();
            }

            var config = new HttpConfiguration();
            var request = new HttpRequestMessage(HttpMethod.Post, "http://localhost/");

            _controller = new UserController
                {
                    Request = request,
                    DocumentStore = DocumentStore
                };

            _controller.Request.Properties[HttpPropertyKeys.HttpConfigurationKey] = config;
        }

        [Test]
        public async Task GetAllWaitingReturnsAllActiveNotApprovedUsers()
        {
            var response =
                await _controller.GetAllWaiting(new UserController.RequestInformation(Database, "emptyToken"));

            var result = await response.Content.ReadAsAsync<ResponseContainer<IList<User>>>(new[]
                {
                    new TextPlainResponseFormatter()
                });

            Assert.That(result.Result.Count, Is.EqualTo(1));
        }

        [Test]
        public async Task GetRolesGetsTheRolesForSpecificUser()
        {
            var response = await
                           _controller.GetRoles(new UserController.RoleRequestInformation(Database, "emptyToken",
                                                                                          "approvedActiveUser@test.com"));

            var result = await response.Content.ReadAsAsync<ResponseContainer<IList<string>>>(new[]
                {
                    new TextPlainResponseFormatter()
                });

            Assert.That(result.Result.Count, Is.EqualTo(2));
            Assert.That(result.Result, Is.EquivalentTo(new List<string> {"admin", "boss"}));
        }

        [Test]
        public async Task GetRolesFailsGracefully()
        {
            var response = await
                           _controller.GetRoles(new UserController.RoleRequestInformation(Database, "emptyToken",
                                                                                          "where@am.i"));

            var result = await response.Content.ReadAsAsync<ResponseContainer<IList<string>>>(new[]
                {
                    new TextPlainResponseFormatter()
                });

            Assert.That(result.Status, Is.EqualTo(404));
            Assert.That(result.Message, Is.EqualTo("User not found."));
        }

        [Test]
        public async Task AcceptUserSetsTheUserAcceptPropertyToTrue()
        {
            var response = await
                           _controller.Accept(new UserController.AcceptRequestInformation(Database, "emptyToken",
                                                                                          "notApprovedActiveUser@test.com",
                                                                                          new Collection<string>
                                                                                              {
                                                                                                  "Monkey"
                                                                                              }));

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NoContent));

            using (var s = DocumentStore.OpenSession())
            {
                var user = s.Query<User, UserByEmailIndex>()
                            .Customize(x => x.WaitForNonStaleResultsAsOfLastWrite())
                            .Single(x => x.Email == "notApprovedActiveUser@test.com".ToLowerInvariant());

                Assert.That(user.Approved, Is.True);
                Assert.That(user.Roles, Is.EquivalentTo(new Collection<string> {"monkey"}));
            }
        }

        [Test]
        public async Task AcceptUserFailsGracefully()
        {
            var response = await
                           _controller.Accept(new UserController.AcceptRequestInformation(Database, "emptyToken",
                                                                                          "where@am.i",
                                                                                          new Collection<string>
                                                                                              {
                                                                                                  "Monkey"
                                                                                              }));

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
        }

        [Test]
        public async Task RejectUserRemovesAllPrivs()
        {
            var response = await
                           _controller.Reject(new UserController.RejectRequestInformation(Database, "emptyToken",
                                                                                          "approvedActiveUser@test.com"));

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NoContent));

            using (var s = DocumentStore.OpenSession())
            {
                var user = s.Query<User, UserByEmailIndex>()
                            .Customize(x => x.WaitForNonStaleResultsAsOfLastWrite())
                            .Single(x => x.Email == "approvedActiveUser@test.com".ToLowerInvariant());

                Assert.That(user.Approved, Is.False);
                Assert.That(user.Active, Is.False);
                Assert.That(user.Roles, Is.Empty);
            }
        }

        [Test]
        public async Task ResetPasswordChangesSaltAndPassword()
        {
            var response = await
                           _controller.ResetPassword(new UserController.ResetRequestInformation(Database, "emptyToken",
                                                                                          "approvedActiveUser@test.com"));

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NoContent));

            using (var s = DocumentStore.OpenSession())
            {
                var user = s.Query<User, UserByEmailIndex>()
                            .Customize(x => x.WaitForNonStaleResultsAsOfLastWrite())
                            .Single(x => x.Email == "approvedActiveUser@test.com".ToLowerInvariant());

                Assert.That(user.Password, Is.Not.EqualTo("password"));
                Assert.That(user.Salt, Is.Not.EqualTo(""));
            }
        }
    }
}