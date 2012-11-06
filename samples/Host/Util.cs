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
using System.Web;
using System.Reflection;
using System.IO;

using CodeSharp.Core.Castles;
//using CodeSharp.Core.ZooKeepers;

namespace Host
{
    public static class Util
    {
        /// <summary>
        /// 获取NTFE-BPM的主引用
        /// </summary>
        /// <returns></returns>
        public static IEnumerable<Assembly> TFlowEngineReference()
        {
            yield return Assembly.Load("Taobao.Workflow.Activities.Repositories");
            yield return Assembly.Load("Taobao.Workflow.Activities.Application");
            yield return Assembly.Load("Taobao.Workflow.Activities");
            //流程解析/转换器实现
            yield return Assembly.Load("Taobao.Workflow.Activities.Converters");
        }

        public static void Resolve(WindsorResolver r)
        {
            var windsor = r.Container;
            //注册EventBus
            windsor.RegisterComponent(typeof(EventBus));
            //注册人工节点超时升级与外围的集成实现
            windsor.RegisterComponent(typeof(HumanEscalationHelper));
            //注册子流程与外围的集成实现
            windsor.RegisterComponent(typeof(SubProcessHelper));
        }
    }
}