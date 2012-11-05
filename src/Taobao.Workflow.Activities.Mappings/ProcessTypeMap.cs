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
using FluentNHibernate.Mapping;
using FluentNHibernate;

namespace Taobao.Workflow.Activities.Mappings
{
    public class ProcessTypeMap : ClassMap<ProcessType>
    {
        public ProcessTypeMap()
        {
            Table("NTFE_ProcessType");
            //HACK:ProcessType启用二级缓存，对象属于不可变
            Cache.ReadWrite().Region("ProcessTypes");
            Id(m => m.ID);
            Map(m => m.Name).Length(50);
            Map(m => m.Description).Length(500);
            Map(m => m.CreateTime);
            Map(m => m.IsCurrent);
            Map(m => m.Version);
            Map(m => m.Group).Column("GroupName").Length(50);

            Component<ProcessType.WorkflowDefinition>(Reveal.Member<ProcessType>("Workflow")
                , m => m.Map(x => x.Serialized)
                    .Column("WorkflowData")
                    //narchar(max) https://github.com/ali-ent/NTFE-BPM/issues/1
                    .CustomType("StringClob"));
            //HACK:https://github.com/codesharp/infrastructure/blob/master/upgrade.md
            //.LazyLoad();

            HasMany<ActivitySetting>(Reveal.Member<ProcessType>("_settings"))
              .Cascade.SaveUpdate()
              .LazyLoad().KeyColumn("ProcessTypeId");
        }
    }
    public class ActivitySettingMap : ClassMap<ActivitySetting>
    {
        public ActivitySettingMap()
        {
            Table("NTFE_Activity");
            Cache.ReadWrite().Region("ProcessTypes");
            Id(m => m.ID);
            Map(m => m.FlowNodeIndex);
            Map(m => m.ActivityName).Length(50);
            Map(m => m.IsChildOfActivity);

            DiscriminateSubClassesOnColumn("SettingType").Length(255);
        }
    }
    public class CustomSettingMap : SubclassMap<CustomSetting>
    {
        public CustomSettingMap()
        {
            Table("NTFE_Activity");

            Component(m => m.StartRule, m =>
            {
                m.Map(x => x.AfterMinutes).Column("StartRule_AfterMinutes").Nullable();
                m.Map(x => x.At).Column("StartRule_At").Nullable();
                m.References(x => x.TimeZone).Column("StartRule_TimeZoneId").LazyLoad();
            });

            Component(m => m.FinishRule, m =>
            {
                m.Map(Reveal.Member<FinishRule>("_scripts")).Column("FinishRule_Script").Length(500);
            });

            DiscriminatorValue("custom");
        }
    }
    public class HumanSettingMap : SubclassMap<HumanSetting>
    {
        public HumanSettingMap()
        {
            Table("NTFE_Activity");

            Map(m => m.SlotCount);
            Map(m => m.SlotMode).CustomType<HumanSetting.SlotDistributionMode>();
            Map(m => m.Url).Length(500);
            Map(Reveal.Member<HumanSetting>("_actions")).Column("Actions").Length(500);

            Component(m => m.ActionerRule, m =>
            {
                m.Map(Reveal.Member<HumanActionerRule>("_scripts")).Column("ActionerRule_Script").Length(500);
            });
            Component(m => m.EscalationRule, m =>
            {
                m.Map(x => x.ExpirationMinutes).Column("EscalationRule_ExpirationMinutes").Nullable();
                m.Map(x => x.NotifyRepeatMinutes).Column("EscalationRule_NotifyRepeatMinutes").Nullable();
                m.Map(x => x.NotifyTemplateName).Column("EscalationRule_NotifyTemplateName").Nullable();
                m.Map(x => x.GotoActivityName).Column("EscalationRule_GotoActivityName").Nullable();
                m.Map(x => x.RedirectTo).Column("EscalationRule_RedirectTo").Nullable();
                m.References(x => x.TimeZone).Column("EscalationRule_TimeZoneId").LazyLoad();
            });

            DiscriminatorValue("human");
        }
    }
    public class ServerSettingMap : SubclassMap<ServerSetting>
    {
        public ServerSettingMap()
        {
            Table("NTFE_Activity");
            Map(m => m.Script).Column("ServerScript").Length(500);
            Map(m => m.ResultTo).Column("ServerResultTo").Length(50);

            DiscriminatorValue("server");
        }
    }
    public class ParallelSettingMap : SubclassMap<ParallelSetting>
    {
        public ParallelSettingMap()
        {
            Table("NTFE_Activity");
            Map(m => m.CompletionCondition).Length(500);

            DiscriminatorValue("parallel");
        }
    }
    public class SubProcessSettingMap : SubclassMap<SubProcessSetting>
    {
        public SubProcessSettingMap()
        {
            Table("NTFE_Activity");
            Map(m => m.SubProcessTypeName).Length(50);

            DiscriminatorValue("subproc");
        }
    }
}
