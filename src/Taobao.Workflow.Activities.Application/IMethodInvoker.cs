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

namespace Taobao.Workflow.Activities.Application
{
    /// <summary>
    /// 脚本方法执行器
    /// </summary>
    public interface IMethodInvoker
    {
        IDictionary<string, Method> GetMethods();
        string Invoke(string method, params object[] args);
    }

    /// <summary>
    /// 脚本方法定义
    /// </summary>
    public class Method
    {
        public string Name { get; set; }
        public string Service { get; set; }
        public string Description { get; set; }
        /// <summary>
        /// 返回类型是否是void
        /// </summary>
        public bool Void { get; set; }
        public Tuple<string, Type, string>[] Parameters { get; set; }

        public Method(string name, string service, params Tuple<string, Type, string>[] parameters)
        {
            this.Name = name;
            this.Service = service;
            this.Parameters = parameters;

        }
    }
}
