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

namespace Taobao.Workflow.Activities
{
    /// <summary>
    /// 静态文本辅助，用于提供各类信息提示文本
    /// </summary>
    public static class Text
    {
        public static readonly string PROCESS_CREATE = "";
        public static readonly string PROCESS_UPDATE = "";
        public static readonly string PROCESS_CHANGETYPE = "";
        public static readonly string PROCESS_CREATED = "";

        public static readonly string PROCESS_GOTO = "";
        public static readonly string PROCESS_CAN_NOT_GOTO = "";
        public static readonly string PROCESS_ACTIVITY_NOT_FOUND = "";
        public static readonly string PROCESS_CAN_NOT_GOTO_CHILD = "";

        public static readonly string PROCESS_NO_ERROR_TO_RETRY = "";

        public static readonly string PROCESS_CAN_NOT_STOP = "";
        public static readonly string PROCESS_CAN_NOT_RESTART = "";
        public static readonly string PROCESS_CAN_NOT_DELETE_WHEN_RUNNING = "";
        public static readonly string PROCESS_CAN_NOT_DELETE_SUB = "";

        public static readonly string PROCESS_CAN_NOT_ROLLBACK = "";
    }
}