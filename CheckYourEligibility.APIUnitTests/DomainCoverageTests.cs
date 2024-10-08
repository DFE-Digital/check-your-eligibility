using AutoFixture;
using CheckYourEligibility.Domain;
using CheckYourEligibility.Domain.Enums;
using CheckYourEligibility.Domain.Requests;
using CheckYourEligibility.Domain.Responses;
using CheckYourEligibility.Services.Interfaces;
using CheckYourEligibility.WebApp.Controllers;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework.Internal;

namespace CheckYourEligibility.APIUnitTests
{
    public class DomainCoverageTests : TestBase.TestBase
    {

        [SetUp]
        public void Setup()
        {
        }

        [TearDown]
        public void Teardown()
        {
        }

        [Test]
        public void Coverage_JwtAuthResponse()
        {
            //arrange 
            // act
            var item = _fixture.Create<JwtAuthResponse>();

            // assert
            item.Should().BeOfType<JwtAuthResponse>();
        }

        [Test]
        public void Coverage_EligibilityCheckHashData()
        {
            //arrange 
            // act
            var item = _fixture.Create<EligibilityCheckHashData>();
            
            // assert
            item.Should().BeOfType<EligibilityCheckHashData>();
        }

        [Test]
        public void Coverage_QueueMessageCheck()
        {
            //arrange 
            // act
            var item = _fixture.Create<QueueMessageCheck>();

            // assert
            item.Should().BeOfType<QueueMessageCheck>();
        }

        [Test]
        public void Coverage_SystemUser()
        {
            //arrange 
            // act
            var item = _fixture.Create<SystemUser>();

            // assert
            item.Should().BeOfType<SystemUser>();
        }

        [Test]
        public void EnumExtension_Description_Exists()
        {
            //arrange 
            // act
            var item = ApplicationStatus.EvidenceNeeded;


            // assert
            item.GetDescription().Should().Be("Evidence Needed");
        }

        [Test]
        public void EnumExtension_Description_DoesNotExists()
        {
            //arrange 
            // act
            var item = AuditType.Application;


            // assert
            item.GetDescription().Should().Be(item.ToString());
        }

        [Test]
        public void EnumExtension_Description_Null()
        {
            //arrange 
            // act
          
            ApplicationStatus? item = null;


            // assert
            item.GetDescription().Should().BeEmpty();
        }
    }
}