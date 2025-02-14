﻿// ----------------------------------------------------------------------------------
//
// Copyright 2012 Microsoft Corporation
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
// http://www.apache.org/licenses/LICENSE-2.0
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
// ----------------------------------------------------------------------------------

namespace Microsoft.WindowsAzure.Commands.Storage.Table.Cmdlet
{
    using Microsoft.WindowsAzure.Commands.Storage.Common;
    using Microsoft.WindowsAzure.Commands.Storage.Model.Contract;
    using Microsoft.Azure.Cosmos.Table;
    using System;
    using System.Management.Automation;
    using System.Security.Permissions;
    using System.Collections.Generic;
    using global::Azure.Data.Tables.Models;

    /// <summary>
    /// remove an azure table
    /// </summary>
    [Cmdlet("Remove", Azure.Commands.ResourceManager.Common.AzureRMConstants.AzurePrefix + "StorageTable", SupportsShouldProcess = true),OutputType(typeof(Boolean))]
    public class RemoveAzureStorageTableCommand : StorageCloudTableCmdletBase
    {
        [Alias("N", "Table")]
        [Parameter(Position = 0, Mandatory = true, HelpMessage = "Table name",
           ValueFromPipeline = true,
           ValueFromPipelineByPropertyName = true)]
        public string Name { get; set; }

        [Parameter(HelpMessage = "Force to remove the table and all content in it")]
        public SwitchParameter Force
        {
            get { return force; }
            set { force = value; }
        }

        private bool force;

        [Parameter(Mandatory = false, HelpMessage = "Return whether the specified table is successfully removed")]
        public SwitchParameter PassThru { get; set; }

        /// <summary>
        /// Initializes a new instance of the RemoveAzureStorageTableCommand class.
        /// </summary>
        public RemoveAzureStorageTableCommand()
            : this(null)
        {
        }

        /// <summary>
        /// Initializes a new instance of the RemoveAzureStorageTableCommand class.
        /// </summary>
        /// <param name="channel">IStorageTableManagement channel</param>
        public RemoveAzureStorageTableCommand(IStorageTableManagement channel)
        {
            Channel = channel;
            EnableMultiThread = false;
        }

        /// <summary>
        /// confirm the remove operation
        /// </summary>
        /// <param name="message">confirmation message</param>
        /// <returns>true if user confirm the remove operation, otherwise false</returns>
        internal virtual bool ConfirmRemove(string message)
        {
            return ShouldProcess(message);
        }

        /// <summary>
        /// remove azure table
        /// </summary>
        /// <param name="name">table name</param>
        /// <returns>
        /// true if the table is removed, false if user cancel the operation,
        /// otherwise throw an exception</returns>
        internal bool RemoveAzureTable(string name)
        {
            if (!NameUtil.IsValidTableName(name))
            {
                throw new ArgumentException(String.Format(Resources.InvalidTableName, name));
            }

            TableRequestOptions requestOptions = RequestOptions;
            CloudTable table = Channel.GetTableReference(name);

            if (!Channel.DoesTableExist(table, requestOptions, TableOperationContext))
            {
                throw new ResourceNotFoundException(String.Format(Resources.TableNotFound, name));
            }

            if (force || TableIsEmpty(table) || ShouldContinue(string.Format("Remove table and all content in it: {0}", name), ""))
            {
                Channel.Delete(table, requestOptions, TableOperationContext);
                return true;
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// remove azure table
        /// </summary>
        /// <param name="channel"></param>
        /// <param name="tableName">table name</param>
        /// <returns>
        /// true if the table is removed, false if user has cancelled the operation,
        /// otherwise throw an exception</returns>
        internal bool RemoveAzureTableV2(IStorageTableManagement localChannel, string tableName)
        {
            if (!NameUtil.IsValidTableName(tableName))
            {
                throw new ArgumentException(String.Format(Resources.InvalidTableName, tableName));
            }

            // check whether table exists
            bool exists = false;
            string query = $"TableName eq '{tableName}'";
            IEnumerable<TableItem> tableItems = localChannel.QueryTables(query, this.CmdletCancellationToken);
            foreach (TableItem tableItem in tableItems)
            {
                exists = true;
                break;
            }

            if (!exists)
            {
                throw new ResourceNotFoundException(String.Format(Resources.TableNotFound, tableName));
            }

            // delete accordingly
            if (force ||
                this.IsTableEmpty(localChannel, tableName, this.CmdletCancellationToken) ||
                ShouldContinue(string.Format("Remove table and all content in it: {0}", tableName), ""))
            {
                localChannel.DeleteTable(tableName, this.CmdletCancellationToken);
                return true;
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// execute command
        /// </summary>
        [PermissionSet(SecurityAction.Demand, Name = "FullTrust")]
        public override void ExecuteCmdlet()
        {
            if (ShouldProcess(Name, "Remove table"))
            {
                string result = string.Empty;

                bool success = this.Channel.IsTokenCredential ?
                    RemoveAzureTableV2(Channel, Name) :
                    RemoveAzureTable(Name);

                if (success)
                {
                    result = String.Format(Resources.RemoveTableSuccessfully, Name);
                }
                else
                {
                    result = String.Format(Resources.RemoveTableCancelled, Name);
                }

                WriteVerbose(result);

                if (PassThru)
                {
                    WriteObject(success);
                }
            }
        }
    }
}
