/*
    Copyright (C) 2012  Alibaba

	This program is free software: you can redistribute it and/or modify
	it under the terms of the GNU General Public License as published by
	the Free Software Foundation, either version 3 of the License, or
	(at your option) any later version.

	This program is distributed in the hope that it will be useful,
	but WITHOUT ANY WARRANTY; without even the implied warranty of
	MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
	GNU General Public License for more details.

	You should have received a copy of the GNU General Public License
	along with this program.  If not, see <http://www.gnu.org/licenses/>.
 */

using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using CodeSharp.Core.Services;
using NUnit.Framework;

namespace Taobao.Workflow.Activities.Test
{
    [Microsoft.VisualStudio.TestTools.UnitTesting.TestClass]
    [TestFixture]
    public class AgentTest : BaseTest
    {
        private User _user, _actAs;

        [Test]
        [Microsoft.VisualStudio.TestTools.UnitTesting.TestMethod]
        public void Create()
        {
            this.Prepare();

            var service = DependencyResolver.Resolve<IAgentService>();

            var agent = new Agent(_user
                , _actAs
                , DateTime.Now
                , DateTime.Now.AddDays(1)
                , ActingRange.All);
            service.Create(agent);

            try
            {
                service.Create(agent);
            }
            catch (Exception e)
            {
                _log.Info(e);
                Assert.IsTrue(true);
            }
        }

        [Test]
        [Microsoft.VisualStudio.TestTools.UnitTesting.TestMethod]
        public void Create_RangeDeclaration()
        {
            this.Prepare();
            this.CreateProcessType(ProcessTypeTest.TYPENAME);

            var service = DependencyResolver.Resolve<IAgentService>();

            var agent = new Agent(_user
                 , _actAs
                , DateTime.Now
                , DateTime.Now.AddDays(1)
                , ActingRange.Declaration
                , this._processTypeService.GetProcessType(ProcessTypeTest.TYPENAME));

            service.Create(agent);
        }

        [Test]
        [Microsoft.VisualStudio.TestTools.UnitTesting.TestMethod]
        public void Get()
        {
            this.PrepareUser();
            var service = DependencyResolver.Resolve<IAgentService>();

            service.GetAgents(_actAs);
            service.GetActings(_user);
        }

        [Test]
        [Microsoft.VisualStudio.TestTools.UnitTesting.TestMethod]
        public void Revoke()
        {
            this.PrepareUser();
            var service = DependencyResolver.Resolve<IAgentService>();
            service.RevokeAll(_user);
            service.RevokeAll(_actAs);

            Assert.AreEqual(0, service.GetAgents(_user).Count());
            Assert.AreEqual(0, service.GetAgents(_actAs).Count());
        }

        private void PrepareUser()
        {
            _user = this._userService.GetUser("houkun");
            _actAs = this._userService.GetUser("xiaoxuan");
        }
        private void Prepare()
        {
            this.PrepareUser();
            DependencyResolver.Resolve<IAgentService>().RevokeAll(_user);
            DependencyResolver.Resolve<IAgentService>().RevokeAll(_actAs);
        }
    }
}
