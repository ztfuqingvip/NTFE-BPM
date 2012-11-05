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

namespace Taobao.Workflow.Activities.Management
{
    /// <summary>
    /// 脚本函数定义
    /// </summary>
    public class ScriptFunction
    {
        /// <summary>
        /// 函数名
        /// </summary>
        public string Name { get; set; }
        /// <summary>
        /// 说明
        /// </summary>
        public string Description { get; set; }
        /// <summary>
        /// 函数参数定义
        /// </summary>
        public ScriptFunctionParameter[] Parameters { get; set; }
        /// <summary>
        /// 是否有返回值
        /// </summary>
        public bool HasReutrnValue { get; set; }
    }
    /// <summary>
    /// 脚本函数参数定义
    /// </summary>
    public class ScriptFunctionParameter
    {
        /// <summary>
        /// 名称
        /// </summary>
        public string Name { get; set; }
        /// <summary>
        /// 描述
        /// </summary>
        public string Description { get; set; }
        /// <summary>
        /// 类型
        /// </summary>
        public string Type { get; set; }
    }
}