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
using Taobao.Workflow.Activities.Hosting;

namespace Taobao.Workflow.Activities.Statements
{
    /// <summary>
    /// 子流程活动
    /// </summary>
    public class SubProcess : Custom
    {
        /// <summary>
        /// 获取或设置完成规则
        /// </summary>
        public IDictionary<string, string> FinishRule { get; set; }
        /// <summary>
        /// 初始化子流程活动
        /// </summary>
        public SubProcess() : base() { }
        /// <summary>
        /// 初始化子流程活动
        /// </summary>
        /// <param name="flowNodeIndex">对应的索引</param>
        public SubProcess(int flowNodeIndex) : base(flowNodeIndex) { }

        protected override void ExecuteBody(NativeActivityContext context)
        {
            //创建子流程节点实例信息
            context.GetExtension<SubProcessExtension>().AddSubProcess(context
                , context.CreateBookmark(this.GenerateBookmarkName(context)
                , this.OnBookmarkCallback)
                , this.DisplayName
                , this.FlowNodeIndex);
        }
        private void OnBookmarkCallback(NativeActivityContext context, Bookmark bookmark, object value)
        {
            //HACK:【重要】子流程节点书签回调逻辑较复杂，容易出错，需要对异常进行处理以便在该回调级别重试而避免整个节点的重试
            this.ExecuteAndDealWithError(() => 
                this.OnBookmarkCallback(context, bookmark)
                , context
                //复用原书签
                , bookmark);
        }
        private void OnBookmarkCallback(NativeActivityContext context, Bookmark bookmark)
        {
            var log = context.Resolve<ILoggerFactory>() != null
                    ? context.Resolve<ILoggerFactory>().Create(typeof(SubProcess))
                    : null;
            var parser = context.Resolve<IScriptParser>();
            var extension = context.GetExtension<DataFieldExtension>();

            //完成规则解析
            if (this.Result != null
                && parser != null
                && this.FinishRule != null
                && this.FinishRule.Count > 0)
            {
                foreach (var i in this.FinishRule)
                    if (parser.EvaluateRule(i.Value, extension))
                    {
                        this.Result.Set(context, i.Key);
                        if (log != null)
                            log.InfoFormat("SubProcess节点完成规则“{0}”测试通过，将进入节点“{1}”", i.Value, i.Key);
                        break;
                    }
            }
            //所有逻辑完成才可删除，若上述逻辑失败则将此书签用于错误恢复
            context.RemoveBookmark(bookmark);
        }
    }
}
