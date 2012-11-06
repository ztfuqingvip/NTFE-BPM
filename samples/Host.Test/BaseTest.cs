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
using NUnit.Framework;
using CodeSharp.ServiceFramework.Castles;
using CodeSharp.ServiceFramework.Interfaces;
using Taobao.Workflow.Activities.Client;

namespace Host.Test
{
    [TestFixture]
    [Microsoft.VisualStudio.TestTools.UnitTesting.TestClass]
    public class BaseTest
    {
        protected string _originator = @"taobao-hz\houkun";
        protected string _superior = @"taobao-hz\djl1854";

        protected ILog _log;
        protected Taobao.Workflow.Activities.Client.ITFlowEngine _clientApi;
        protected Taobao.Workflow.Activities.Management.ITFlowEngine _managementApi;

        [TestFixtureSetUp]
        [Microsoft.VisualStudio.TestTools.UnitTesting.TestInitialize]
        public void Config()
        {
            var c = new Castle.Windsor.WindsorContainer();
            var endpoint = CodeSharp.ServiceFramework.Configuration
                .Configure()
                .Castle(c)
                .Log4Net(true)
                .Associate(new Uri(System.Configuration.ConfigurationManager.AppSettings["CenterUri"]))
                .Identity(new CodeSharp.ServiceFramework.Identity() { Source = "NTFE-BPM", AuthKey = "75DC6B572D1B940E34159DCD7FF26D8D" })
                .Endpoint();
            endpoint.Run();

            this._log = endpoint.Resolve<ILoggerFactory>().Create(this.GetType());
            this._clientApi = c.Resolve<Taobao.Workflow.Activities.Client.ITFlowEngine>();
            this._managementApi = c.Resolve<Taobao.Workflow.Activities.Management.ITFlowEngine>();

            //NTFE使用ID作为账号库用户名
            //UDONE:由于人员服务未做mock，因此目前取固定值
            this._originator = "5FE9A969-7CB2-4BA7-9601-E11473E8B233";//this.GetMappedUserId(this._originator).ToString();
            this._superior = "9D23EAA9-6145-4635-A7C2-D8AEEDF45C1E";//this.GetMappedUserId(this._superior).ToString();
        }
        [TestFixtureTearDown]
        [Microsoft.VisualStudio.TestTools.UnitTesting.TestCleanup]
        public void Cleanup()
        {
            CodeSharp.ServiceFramework.Configuration.Cleanup();
        }

        protected void Idle()
        {
            this.Idle(10000);
        }
        protected void Idle(int i)
        {
            System.Threading.Thread.Sleep(i);
        }
        protected void CreateProcessType(string typeName, string workflow, string settings)
        {
            //发布流程
            this._managementApi.CreateProcessType(typeName, workflow, settings, "对引擎客户端进行集成测试的流程", "test");
        }
        protected Process CreateProcess(string typeName, IDictionary<string, string> dict)
        {
            return this._clientApi.NewProcess(typeName
               , "From Engine Client UnitTest at " + DateTime.Now
               , this._originator
               , 0
               , dict);
        }
    }
}