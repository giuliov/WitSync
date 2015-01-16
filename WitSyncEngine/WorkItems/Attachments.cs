using Microsoft.TeamFoundation.WorkItemTracking.Client;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WitSync
{
    partial class WorkItemMapper
    {
        class AttachmentComparer : IEqualityComparer<Attachment>
        {
            public bool Equals(Attachment x, Attachment y)
            {

                //Check whether the compared objects reference the same data.
                if (Object.ReferenceEquals(x, y)) return true;

                //Check whether any of the compared objects is null.
                if (Object.ReferenceEquals(x, null) || Object.ReferenceEquals(y, null))
                    return false;

                //Check whether the objects' properties are equal.
                return x.Name == y.Name && x.Length == y.Length;
            }

            // If Equals() returns true for a pair of objects 
            // then GetHashCode() must return the same value for these objects.
            public int GetHashCode(Attachment a)
            {
                //Check whether the object is null
                if (Object.ReferenceEquals(a, null)) return 0;

                //Calculate the hash code for the object. 
                // (bless StackOverflow)
                // http://stackoverflow.com/questions/263400/what-is-the-best-algorithm-for-an-overridden-system-object-gethashcode
                unchecked // Overflow is fine, just wrap
                {
                    int hash = 17;
                    // Suitable nullity checks etc, of course :)
                    hash = hash * 23 + a.Name.GetHashCode();
                    hash = hash * 23 + a.Length.GetHashCode();
                    return hash;
                }
            }
        }

        class AttachmentEnumerable : IEnumerable<Attachment>
        {
            private AttachmentCollection underlyingCollection;

            public AttachmentEnumerable(AttachmentCollection coll)
            { underlyingCollection = coll; }

            public IEnumerator<Attachment> GetEnumerator()
            {
                foreach (Attachment item in underlyingCollection)
                {
                    yield return item;
                }
            }

            System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
            {
                return this.GetEnumerator();
            }
        }

        private void SetAttachments(WorkItem source, WorkItemMap map, WorkItem target)
        {
            if (map.Attachments == WorkItemMap.AttachmentMode.DoNotSync)
                return;

            var srcColl = new AttachmentEnumerable(source.Attachments);
            var dstColl = new AttachmentEnumerable(target.Attachments);
            var comparer = new AttachmentComparer();

            if ((map.Attachments & WorkItemMap.AttachmentMode.ClearTarget) == WorkItemMap.AttachmentMode.ClearTarget)
            {
                target.Attachments.Clear();
            }
            else if ((map.Attachments & WorkItemMap.AttachmentMode.RemoveIfAbsent) == WorkItemMap.AttachmentMode.RemoveIfAbsent)
            {
                var onlyInTarget = dstColl.Except(srcColl, comparer).ToList();

                // remove
                foreach (var a in onlyInTarget)
                {
                    try
                    {
                        target.Attachments.Remove(a);
                    }
                    catch (Exception ex)
                    {
                        this.EventSink.ExceptionWhileRemovingAttachment(ex, a, target);
                    }//try
                }//for
            }//if

            if ((map.Attachments & WorkItemMap.AttachmentMode.AddAndUpdate) == WorkItemMap.AttachmentMode.AddAndUpdate)
            {
                var onlyInSource = srcColl.Except(dstColl, comparer).ToList();

                //add
                foreach (var sourceAttachment in onlyInSource)
                {
                    try
                    {
                        // see http://stackoverflow.com/questions/3507939/how-can-i-add-an-attachment-via-the-sdk-to-a-work-item-without-using-a-physical
                        string tempFile = DownloadAttachment(sourceAttachment);
                        Attachment newAttachment = new Attachment(tempFile, sourceAttachment.Comment);
                        // TODO check SourceStore.MaxBulkUpdateBatchSize vs DestinationStore.MaxBulkUpdateBatchSize
                        target.Attachments.Add(newAttachment);
                    }
                    catch (Exception ex)
                    {
                        this.EventSink.ExceptionWhileAddingAttachment(ex, sourceAttachment, source);
                    }//try
                }//for
            }//if
        }

        private string DownloadAttachment(Attachment sourceAttachment)
        {
            // TODO optimize using tip from http://www.timschaeps.com/team-foundation-service-downloading-attachments-from-work-items-through-the-api/
            var wc = new System.Net.WebClient();
            var cred = this.SourceConnection.Credential;
            if (cred != null
                && !string.IsNullOrWhiteSpace(cred.UserName))
            {
                wc.Credentials = this.SourceConnection.Credential;
                wc.UseDefaultCredentials = false;
            }
            else
            {
                wc.UseDefaultCredentials = true;
            }

            // two attachments may have the same name, so we generate a unique path (suboptimal)
            string tempFolder = Path.Combine(GetTemporaryAttachmentFolder(), sourceAttachment.FileGuid);
            Directory.CreateDirectory(tempFolder);
            string tempFile = Path.Combine(tempFolder, sourceAttachment.Name);

            wc.DownloadFile(sourceAttachment.Uri, tempFile);
            return tempFile;
        }

        internal void CleanUpAttachments()
        {
            if (Directory.Exists(GetTemporaryAttachmentFolder()))
            {
                // cleanup attachment temp
                Directory.Delete(GetTemporaryAttachmentFolder(), true);
            }
        }

        private string GetTemporaryAttachmentFolder()
        {
            return Path.Combine(Path.GetTempPath(), "WitSyncAttachments");
        }
    }
}