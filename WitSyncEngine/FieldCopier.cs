using Microsoft.TeamFoundation.WorkItemTracking.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WitSync
{
    class FieldCopier
    {
        class CopyTask
        {
            internal string SourceFieldName;
            internal string TargetFieldName;
            internal Action<Field, Field> CopyAction;
        }

        Dictionary<FieldDefinition, CopyTask> tasks = new Dictionary<FieldDefinition, CopyTask>();
        List<CopyTask> unboundTasks = new List<CopyTask>();

        public FieldCopier(ProjectMapping mapping, MapperFunctions functions, bool useEditableProperty, WorkItemType sourceType, WorkItemMap map, WorkItemType targetType, IEngineEvents engineEvents)
        {
            engineEvents.Trace("Interpreting rules for mapping '{0}' workitems to '{1}'", sourceType.Name, targetType.Name);

            foreach (FieldDefinition fromField in sourceType.FieldDefinitions)
            {
                var rule = map.FindFieldRule(fromField.ReferenceName);
                if (rule == null)
                {
                    // if no rule -> skip field
                    engineEvents.NoRuleFor(sourceType, fromField.ReferenceName);
                    continue;
                }
                string targetFieldName
                    = rule.IsWildcard
                    ? fromField.ReferenceName : rule.Destination;
                if (string.IsNullOrWhiteSpace(rule.Destination))
                {
                    engineEvents.Trace("RULE: Skip {0}", fromField.ReferenceName);
                    continue;
                }
                if (!targetType.FieldDefinitions.Contains(targetFieldName))
                {
                    engineEvents.Trace("RULE: Skip {0} (Target field {1} does not exist)", fromField.ReferenceName, targetFieldName);
                    continue;
                }

                var toField = targetType.FieldDefinitions[targetFieldName];

                if (!IsAssignable(useEditableProperty, fromField, toField))
                {
                    engineEvents.Trace("RULE: Skip {0} (Not assignable to {1})", fromField.ReferenceName, targetFieldName);
                    continue;
                }//if

                // make the proper copier function
                Action<Field, Field> copyAction;

                if (rule.IsWildcard)
                {
                    engineEvents.Trace("RULE: Copy {0} to {1} (Wildcard)", fromField.ReferenceName, targetFieldName);
                    copyAction = (src, dst) => { dst.Value = src.Value; };
                }
                else if (!string.IsNullOrWhiteSpace(rule.Set))
                {
                    engineEvents.Trace("RULE: Set {0} to value '{1}'", targetFieldName, rule.Set);
                    copyAction = (src, dst) => { SetFieldWithConstant(dst, rule.Set); };
                }
                else if (!string.IsNullOrWhiteSpace(rule.SetIfNull))
                {
                    engineEvents.Trace("RULE: Set {0} to value '{1}' when source is null", targetFieldName, rule.SetIfNull);
                    copyAction = (src, dst) =>
                    {
                        if (src.Value == null)
                        {
                            SetFieldWithConstant(dst, rule.SetIfNull);
                        }
                        else
                        {
                            dst.Value = src.Value;
                        }
                    };
                }
                else if (!string.IsNullOrWhiteSpace(rule.Translate))
                {
                    var flags = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic;
                    // TODO optimize
                    var translatorMethod = functions.GetType().GetMethod(rule.Translate, flags);
                    if (translatorMethod == null)
                    {
                        engineEvents.TranslatorFunctionNotFoundUsingDefault(rule);
                        // default: no translation
                        engineEvents.Trace("RULE: Copy {0} to {1} (fallback)", fromField.ReferenceName, targetFieldName);
                        copyAction = (src, dst) => { dst.Value = src.Value; };
                    }
                    else
                    {
                        engineEvents.Trace("RULE: Translate {0} via {1}", targetFieldName, rule.Translate);
                        copyAction = (src, dst) =>
                        {
                            dst.Value = translatorMethod.Invoke(functions, new object[] { rule, map, mapping, src.Value });
                        };
                    }
                }
                else
                {
                    //engineEvents.InvalidRule(rule);
                    engineEvents.Trace("RULE: Copy {0} to {1} (Explicit)", fromField.ReferenceName, targetFieldName);
                    // crossing fingers
                    copyAction = (src, dst) => { dst.Value = src.Value; };
                }//if

                tasks.Add(fromField, new CopyTask()
                {
                    SourceFieldName = fromField.ReferenceName,
                    CopyAction = copyAction,
                    TargetFieldName = targetFieldName
                });

            }//for fields

            // now the Set rules!
            foreach (var rule in map.Fields)
            {
                if (string.IsNullOrWhiteSpace(rule.Source))
                {
                    engineEvents.Trace("RULE: Set {0} to value '{1}'", rule.Destination, rule.Set);
                    unboundTasks.Add(new CopyTask()
                    {
                        SourceFieldName = string.Empty,
                        CopyAction = (src, dst) => { SetFieldWithConstant(dst, rule.Set); },
                        TargetFieldName = rule.Destination
                    });
                }//if
            }//for
        }

        private bool IsAssignable(bool useEditableProperty, FieldDefinition fromField, FieldDefinition toField)
        {
            if (useEditableProperty)
            {
                return toField.IsEditable;
            }
            else
            {
                if (toField.IsComputed)
                    return false;
                if (toField.ReferenceName == "System.CreatedBy")
                    return false;
                return true;
            }//if
        }

        public void Copy(WorkItem source, WorkItem target, IEngineEvents engineEvents)
        {
            foreach (Field fromField in source.Fields)
            {
                CopyTask copier;
                if (tasks.TryGetValue(fromField.FieldDefinition, out copier))
                {
                    engineEvents.Trace("Source field {0} has value '{1}'", fromField.ReferenceName, fromField.Value);
                    copier.CopyAction(fromField, target.Fields[copier.TargetFieldName]);
                }
            }//for fields
            foreach (var task in unboundTasks)
            {
                task.CopyAction(null, target.Fields[task.TargetFieldName]);
            }
        }

        private static void SetFieldWithConstant(Field toField, string constant)
        {
            // fixed value
            switch (toField.FieldDefinition.FieldType)
            {
                // TODO source field is not needed
                // Parse always succeeds, as value is already validated by Checker
                case FieldType.Boolean:
                    toField.Value = bool.Parse(constant);
                    break;
                case FieldType.DateTime:
                    toField.Value = DateTime.Parse(constant);
                    break;
                case FieldType.Double:
                    toField.Value = double.Parse(constant);
                    break;
                case FieldType.Guid:
                    toField.Value = Guid.Parse(constant);
                    break;
                case FieldType.Integer:
                    toField.Value = int.Parse(constant);
                    break;
                default:
                    toField.Value = constant;
                    break;
            }//switch
        }
    }
}
