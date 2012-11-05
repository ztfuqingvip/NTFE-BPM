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
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using CodeSharp.Core.Services;
using CodeSharp.Core;

namespace Taobao.Workflow.Activities.Application
{
    /// <summary>
    /// 默认脚本解析器
    /// </summary>
    [CodeSharp.Core.Component]
    public class ScriptParser : IScriptParser
    {
        private ILog _log;
        private IUserHelper _userHelper;
        private IMethodInvoker _invoker;
        private CodeSharp.Core.Utils.Serializer _serializer = new CodeSharp.Core.Utils.Serializer();

        public ScriptParser(ILoggerFactory factory, IUserHelper userHelper, IMethodInvoker invoker)
        {
            this._log = factory.Create(typeof(ScriptParser));
            this._userHelper = userHelper;
            this._invoker = invoker;
        }

        #region IScriptParser Members
        //用于执行完成规则
        bool IScriptParser.EvaluateFinishRule(WorkItem workItem
            , List<DefaultHumanFinishRuleAsserter.Execution> allOtherExecutions
            , IDictionary<string, string> inputs
            , string action
            , string script)
        {
            var setting = workItem.GetReferredSetting();
            //断言
            var asserter = new DefaultHumanFinishRuleAsserter(setting.IsUsingSlot ? setting.SlotCount : new int?()
                , new DefaultHumanFinishRuleAsserter.Execution(action, WorkItemStatus.Executed)
                , allOtherExecutions);

            //初始化脚本引擎
            var engine = new Jurassic.ScriptEngine();
            //设置表达式
            engine.SetGlobalFunction("all", new Func<string, bool>(asserter.All));
            engine.SetGlobalFunction("atLeaseOf", new Func<int, string, bool>(asserter.AtLeaseOf));
            engine.SetGlobalFunction("atMostOf", new Func<int, string, bool>(asserter.AtMostOf));
            engine.SetGlobalFunction("noneOf", new Func<string, bool>(asserter.NoneOf));
            //填充变量
            foreach (var pair in inputs)
                engine.SetGlobalValue(pair.Key, pair.Value ?? "");

            return this.Evaluate<bool>(engine, script);
        }
        //用户获取用户列表
        string[] IScriptParser.EvaluateUsers(string script, Statements.DataFieldExtension extension)
        {
            var result = this.Evaluate<object>(this.PrepareScriptEngine(extension), script);

            if (this._log.IsDebugEnabled)
                this._log.DebugFormat("执行人规则“{0}”解析结果={1}", script, result);

            if (result is Jurassic.Library.ArrayInstance)
                return (result as Jurassic.Library.ArrayInstance).ElementValues.Select(o => o.ToString()).ToArray();

            if (result.GetType() != typeof(string))
                throw new InvalidOperationException(string.Format("脚本“{0}”解析后返回了目前不支持的类型{1}", script, result));

            var jsonUser = result.ToString();

            if (string.IsNullOrEmpty(jsonUser))
                return null;
            //TODO:需要合理反序列化
            return jsonUser.StartsWith("[")
                ? this._serializer.JsonDeserialize<string[]>(jsonUser)
                : (jsonUser.StartsWith("\"") ? new string[] { this._serializer.JsonDeserialize<string>(jsonUser) } : new string[] { jsonUser });
        }
        //用于执行普通脚本
        string IScriptParser.Evaluate(string script, Statements.DataFieldExtension extension)
        {
            var result = this.Evaluate<string>(this.PrepareScriptEngine(extension), script);

            if (this._log.IsDebugEnabled)
                this._log.DebugFormat("脚本“{0}”解析结果={1}", script, result);

            //特化处理forward函数的返回值
            if (script.StartsWith("forward("))
                return result;
            try
            {
                //由于脚本都是通过NSF调用外部服务，返回的字符串均是json结果，需要反序列化
                return !string.IsNullOrEmpty(result) && result != "undefined"
                    ? this._serializer.JsonDeserialize<string>(result)
                    : string.Empty;
            }
            catch (Exception e)
            {
                if (this._log.IsDebugEnabled)
                    this._log.Debug("反序列字符串异常，直接返回原始文本：" + result, e);
                return result;
            }
        }
        //用于执行完成规则脚本
        bool IScriptParser.EvaluateRule(string script, Statements.DataFieldExtension extension)
        {
            return this.Evaluate<bool>(this.PrepareScriptEngine(extension), script);
        }
        #endregion

        private Jurassic.ScriptEngine PrepareScriptEngine(Statements.DataFieldExtension extension)
        {
            var engine = new Jurassic.ScriptEngine();
            //注册方法列表
            this._invoker.GetMethods().ToList().ForEach(o =>
                engine.SetGlobalFunction(o.Key
                    , new Func<object, object, object, object
                    //, object, object, object, object, object
                        , string>(
                        (v1, v2, v3, v4) =>
                            //, v5, v6, v7, v8, v9) =>
                            this.Invoke(o.Key, v1, v2, v3, v4))));
            //, v5, v6, v7, v8, v9))));
            
            //注册内置方法 会覆盖上述方法
            engine.SetGlobalFunction("updateDataField", new Action<string, string>((k, v) => extension.Set(k, v)));
            engine.SetGlobalFunction("split"
                , new Func<string, string, Jurassic.Library.ArrayInstance>((s, i) => 
                    engine.Array.Call((i ?? "").Split(new string[] { s }, StringSplitOptions.RemoveEmptyEntries))));

            //注册依赖上下文的内置方法 会覆盖上述方法
            engine.SetGlobalFunction("getSuperior", new Func<string>(() => this._userHelper.GetSuperior(extension.Originator)));
            
            //注册流程变量
            foreach (var pair in extension.DataFields)
                engine.SetGlobalValue(pair.Key, pair.Value ?? "");
            
            //注册内置上下文变量 会覆盖上述流程变量
            engine.SetGlobalValue(WorkflowBuilder.Variable_Originator, extension.Originator);

            return engine;
        }
        private T Evaluate<T>(Jurassic.ScriptEngine engine, string script)
        {
            try
            {
                return engine.Evaluate<T>(script);
            }
            catch (Exception e)
            {
                this._log.Error("脚本解析异常", e);
                throw new Exception("脚本解析异常：" + e.Message);
            }
        }

        //由于目前的脚本引擎限制而用此设计，同时参数数量有限制
        public string Invoke(string method
            , object v1, object v2, object v3, object v4)
        //, object v5, object v6, object v7, object v8, object v9)
        {
            return this._invoker.Invoke(method, v1, v2, v3, v4);
            //, v5, v6, v7, v8, v9);
        }
    }
}