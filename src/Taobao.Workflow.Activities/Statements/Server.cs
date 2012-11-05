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
using Taobao.Activities;
using CodeSharp.Core.Services;

namespace Taobao.Workflow.Activities.Statements
{
    /// <summary>
    /// 服务端活动 自动执行
    /// </summary>
    public class Server : Custom
    {
        /// <summary>
        /// 获取或设置执行内容
        /// </summary>
        public InArgument<string> Script { get; set; }
        /// <summary>
        /// 获取执行结果的输出目标
        /// </summary>
        public OutArgument<string> To { get; private set; }
        /// <summary>
        /// 获取或设置完成规则
        /// </summary>
        public IDictionary<string, string> FinishRule { get; set; }

        private string _to;

        /// <summary>
        /// 初始化
        /// </summary>
        public Server()
            : base()
        {
            this.Script = new InArgument<string>();
        }
        /// <summary>
        /// 初始化
        /// </summary>
        /// <param name="flowNodeIndex">对应的索引</param>
        /// <param name="actioners"></param>
        public Server(int flowNodeIndex)
            : base(flowNodeIndex)
        {
            this.Script = new InArgument<string>();
        }

        /// <summary>
        /// 设置脚本执行结果输出到指定变量
        /// </summary>
        /// <param name="name"></param>
        /// <param name="to"></param>
        public void SetScriptResultTo(Variable<string> to)
        {
            this._to = to.Name;
            this.To = new OutArgument<string>(to);
        }

        protected override void ExecuteBody(NativeActivityContext context)
        {
            var log = context.Resolve<ILoggerFactory>() == null
                ? null
                : context.Resolve<ILoggerFactory>().Create(typeof(Server));

            //创建server节点实例
            var server = context.GetExtension<ServerExtension>().AddServer(context, this.DisplayName, this.FlowNodeIndex);
            var data = context.GetExtension<DataFieldExtension>();
            //设置当前节点索引
            data.SetCurrentNode(this.FlowNodeIndex);

            var serverScript = this.Script == null ? null : this.Script.Get(context);
            var parser = context.Resolve<IScriptParser>();

            #region 执行内容解析
            if (!string.IsNullOrEmpty(serverScript))
            {
                var result = parser == null
                    ? serverScript
                    : parser.Evaluate(serverScript, context.GetExtension<DataFieldExtension>());
                if (this.To != null)
                {
                    this.To.Set(context, result);
                    //更新到流程变量中
                    data.Set(this._to, result);
                }

                log.InfoFormat("Server节点执行内容：{0}，返回：{1} {2}"
                    , serverScript
                    , result
                    , this.To == null ? "" : "将返回值写入流程变量" + this._to);
            }
            #endregion

            #region 完成规则解析
            if (this.Result != null
                && parser != null
                && this.FinishRule != null
                && this.FinishRule.Count > 0)
            {
                foreach (var i in this.FinishRule)
                    if (parser.EvaluateRule(i.Value, data))
                    {
                        this.Result.Set(context, i.Key);
                        log.InfoFormat("Server节点完成规则“{0}”测试通过，将进入节点“{1}”", i.Value, i.Key);
                        break;
                    }
            }
            #endregion

            //置为完成
            server.SetAsComplete();
        }
    }
}