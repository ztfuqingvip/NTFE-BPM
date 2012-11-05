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
using System.Web.Mvc;

namespace Taobao.Workflow.Activities.AdminWeb.Controllers
{
    public class BaseController : Controller
    {
        /// <summary>
        /// 验证是否能够操作
        /// </summary>
        /// <param name="middleWebWhiteList"></param>
        protected void ValidateOperate(string middleWebWhiteList)
        {
            if (!middleWebWhiteList.Split('|').ToList()
                .Exists(o => o.ToLower() 
                    == System.Web.HttpContext.Current.User.Identity.Name.ToLower()))
                throw new Exception("您没有权限执行更新");
        }
    }
}
