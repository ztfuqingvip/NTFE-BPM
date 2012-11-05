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
using System.Diagnostics;
using CodeSharp.Core.Services;
using Castle.MicroKernel.Registration;
using Taobao.Workflow.Activities.Application;
using CodeSharp.Core.Castles;
using NUnit.Framework;

namespace Taobao.Workflow.Activities.Test
{
    [Microsoft.VisualStudio.TestTools.UnitTesting.TestClass]
    [TestFixture]
    public class ScriptParserTest : BaseTest
    {
        protected override void Resolve(Castle.Windsor.IWindsorContainer windsor)
        {
            //使用nsf人员库访问实现
            windsor.RegisterComponent(typeof(DefaultMethodHelper));
            base.Resolve(windsor);
        }

        [Test]
        [Microsoft.VisualStudio.TestTools.UnitTesting.TestMethod]
        public void EngineTest()
        {
            var engine = new Jurassic.ScriptEngine();
            engine.SetGlobalValue("i", 1);
            engine.SetGlobalValue("i", 2);
            Assert.AreEqual(2, engine.GetGlobalValue("i"));

            engine.SetGlobalFunction("test", new Func<object, object, object, object, string>(Test));

            Trace.WriteLine(engine.Evaluate<string>("test(1)"));
            Trace.WriteLine(engine.CallGlobalFunction<string>("test", new object[] { 1 }));

            Trace.WriteLine(engine.Array.Call(1, 2, 3).ElementValues.ToList()[2]);
        }
        [Test]
        [Microsoft.VisualStudio.TestTools.UnitTesting.TestMethod]
        public void Asserter_NoSlot()
        {
            var assert = FinishRuleAsserter(null
                , E("1", WorkItemStatus.Executed)
                , E("1", WorkItemStatus.Executed)
                , E("1", WorkItemStatus.Executed));
            Assert.IsTrue(assert.All("1"));
            Assert.IsTrue(assert.AtLeaseOf(1, "1"));
            Assert.IsTrue(assert.AtMostOf(1, "1"));
            Assert.IsFalse(assert.NoneOf("1"));

            assert = FinishRuleAsserter(null
                , E("1", WorkItemStatus.Executed)
                , E("1", WorkItemStatus.Executed)
                , E("1", WorkItemStatus.New));
            Assert.IsFalse(assert.All("1"));
            Assert.IsFalse(assert.AtLeaseOf(1, "1"));
            Assert.IsTrue(assert.AtMostOf(1, "1"));
            Assert.IsFalse(assert.NoneOf("1"));
        }
        [Test]
        [Microsoft.VisualStudio.TestTools.UnitTesting.TestMethod]
        public void Asserter_Slot()
        {
            var assert = FinishRuleAsserter(3
                , E("1", WorkItemStatus.Executed)
                , E("1", WorkItemStatus.Executed)
                , E("2", WorkItemStatus.Executed));
            Assert.IsFalse(assert.All("1"));
            Assert.IsTrue(assert.AtLeaseOf(1, "1"));
            Assert.IsTrue(assert.AtMostOf(1, "1"));
            Assert.IsFalse(assert.NoneOf("1"));

            assert = FinishRuleAsserter(3
                , E("1", WorkItemStatus.Executed)
                , E("1", WorkItemStatus.New)
                , E("2", WorkItemStatus.Executed));
            Assert.IsFalse(assert.All("1"));
            Assert.IsFalse(assert.AtLeaseOf(2, "1"));
            Assert.IsTrue(assert.AtMostOf(1, "1"));
            Assert.IsFalse(assert.NoneOf("1"));

            assert = FinishRuleAsserter(2
               , E("1", WorkItemStatus.Executed)
               , E("1", WorkItemStatus.Executed)
               , E("2", WorkItemStatus.Executed));
            Assert.IsTrue(assert.All("1"));
            Assert.IsTrue(assert.AtMostOf(1, "1"));
            Assert.IsTrue(assert.AtMostOf(2, "1"));

            //
            assert = FinishRuleAsserter(5
             , E("1", WorkItemStatus.Executed)
             , E("1", WorkItemStatus.Executed)
             , E("1", WorkItemStatus.Executed));
            Assert.IsTrue(assert.AtMostOf(2, "1"));

        }
        [Test]
        [Microsoft.VisualStudio.TestTools.UnitTesting.TestMethod]
        public void Asserter_ActionerLessThanSlot()
        {
            //人数少于slot
            var assert = FinishRuleAsserter(3
                , E("1", WorkItemStatus.Executed)
                , E("1", WorkItemStatus.Executed));
            Assert.IsTrue(assert.AtMostOf(1, "1"));
            Assert.IsTrue(assert.All("1"));
        }
        [Test]
        [Microsoft.VisualStudio.TestTools.UnitTesting.TestMethod]
        public void ScriptMethodTest()
        {
            var p = DependencyResolver.Resolve<IScriptParser>();
            var e = new Statements.DataFieldExtension(@"taobao-hz\houkun", 0, new Dictionary<string, string>() { { "key", "1" } });
            this.TraceArray(p.EvaluateUsers("getSuperior()", e));
            this.TraceArray(p.EvaluateUsers("getSuperiors(originator,1,0)", e));
            this.TraceArray(p.EvaluateUsers("getSuperiorsInRole(originator,1,'BI_Manager')", e));
            this.TraceArray(p.EvaluateUsers("getUpperSuperior(originator,2,0)", e));
            this.TraceArray(p.EvaluateUsers(string.Format("getUsers('BI_Manager',originator,'{0}')", Guid.NewGuid()), e));

            //updateDataField
            Trace.WriteLine(p.Evaluate("updateDataField('key','2')", e));
            Assert.AreEqual("2", e.DataFields["key"]);
            //split
            var ary = p.EvaluateUsers("split(';','f1;f2,;f3,')", e);
            Assert.AreEqual(3, ary.Length);
            Assert.AreEqual("f1", ary[0]);
            Assert.AreEqual("f3,", ary[2]);


            Trace.WriteLine(p.Evaluate("getSuperior()", e));
            Trace.WriteLine(p.Evaluate("forward('JustTest1','{i:1}','{i:2}')", e));
        }

        //对DefaultMethodHelper的测试
        [Test]
        [Microsoft.VisualStudio.TestTools.UnitTesting.TestMethod]
        public void MethodTest()
        {
            var helper = DependencyResolver.Resolve<DefaultMethodHelper>();
            _log.Info(helper.GetSuperior(@"taobao-hz\houkun"));

            helper.GetMethods().ToList().ForEach(o =>
                _log.InfoFormat("{0}|{1}|{2}|{3}"
                , o.Key
                , o.Value.Name
                , o.Value.Service
                , o.Value.Description));

            //forward
            _log.Info(helper.Invoke("forward", "JustTest1", "{}", "{\"i\":\"1\"}"));
        }

        public string Test(object v1, object v2, object v3, object v4)
        {
            return v1.ToString();
        }
        public string Test(string input)
        {
            return input;
        }

        private DefaultHumanFinishRuleAsserter FinishRuleAsserter(int? slot
            , DefaultHumanFinishRuleAsserter.Execution e
            , params DefaultHumanFinishRuleAsserter.Execution[] es)
        {
            return new DefaultHumanFinishRuleAsserter(slot, e, es.ToList());
        }
        private List<DefaultHumanFinishRuleAsserter.Execution> ES(params DefaultHumanFinishRuleAsserter.Execution[] es)
        {
            return es.ToList();
        }
        private DefaultHumanFinishRuleAsserter.Execution E(string action, WorkItemStatus status)
        {
            return new DefaultHumanFinishRuleAsserter.Execution(action, status);
        }

        private void TraceArray(string[] arr)
        {
            foreach (var i in arr)
                _log.Info(i);
        }
    }
}