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
using NUnit.Framework;

namespace Taobao.Workflow.Activities.Test
{
    [Microsoft.VisualStudio.TestTools.UnitTesting.TestClass]
    [TestFixture]
    public class TimeZoneTest : BaseTest
    {
        private static readonly string _default = "Default";
        private TimeZone CreateTimeZone()
        {
            if (this._timeZoneService.GetTimeZone(_default) != null)
                return this._timeZoneService.GetTimeZone(_default);
            var timeZone = new TimeZone(_default);
            foreach (DayOfWeek dayOfWeek in Enum.GetValues(typeof(DayOfWeek)))
            {
                if (dayOfWeek != DayOfWeek.Sunday && dayOfWeek != DayOfWeek.Saturday)
                {
                    timeZone.AddRangeTime(dayOfWeek, 9, 12);
                    timeZone.AddRangeTime(dayOfWeek, 13, 19);
                    timeZone.RemoveRangeTime(dayOfWeek, 18.5, 19);
                }
            }
            timeZone.AddSpecialDay(DateTime.Now, 6, 12);
            timeZone.AddExceptionDay(DateTime.Now.AddDays(1));
            this._timeZoneService.Create(timeZone);
            return timeZone;
        }
        [Test]
        [Microsoft.VisualStudio.TestTools.UnitTesting.TestMethod]
        public void Create()
        {
            var timeZone = this.CreateTimeZone();
            Assert.IsNotNull(timeZone);
        }
        [Test]
        [Microsoft.VisualStudio.TestTools.UnitTesting.TestMethod]
        public void RangeTime()
        {
            var timeZone = new TimeZone("Default");
            timeZone.AddRangeTime(DayOfWeek.Monday, 9, 10);
            Assert.AreEqual(1, timeZone.WorkingDays.Count());
            var workingDay = timeZone.WorkingDays.FirstOrDefault();
            Assert.AreEqual(2, workingDay.WorkingHours.Count());
            timeZone.AddRangeTime(DayOfWeek.Monday, 9, 10);
            Assert.AreEqual(1, timeZone.WorkingDays.Count());
            workingDay = timeZone.WorkingDays.FirstOrDefault();
            Assert.AreEqual(2, workingDay.WorkingHours.Count());
        }
        [Test]
        [Microsoft.VisualStudio.TestTools.UnitTesting.TestMethod]
        public void GetStartTime()
        {
            this.CreateTimeZone();
            var timeZone = this._timeZoneService.GetTimeZone(_default);
            var dateTime = timeZone.CalculateDateTime(TimeSpan.FromMinutes(20));
            Trace.WriteLine(dateTime);
        }
        [Test]
        [Microsoft.VisualStudio.TestTools.UnitTesting.TestMethod]
        public void Delete()
        {
            //var timeZone = this.CreateTimeZone();
            var timeZone = this._timeZoneService.GetTimeZone("Default");
            this._timeZoneService.Delete(timeZone);
        }
    }
}