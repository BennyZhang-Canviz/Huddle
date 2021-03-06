﻿using Huddle.Common;
using Huddle.MetricWebApp.Infrastructure;
using Huddle.MetricWebApp.Models;
using Microsoft.SharePoint.Client;
using System.Linq;
using System.Threading.Tasks;

namespace Huddle.MetricWebApp.SharePoint
{
    public class IssuesService
    {
        public static async Task<Issue[]> GetItemsAsync(int state,string teamId)
        {
            var filter = string.Format(@"<And>
                    <Eq>
                        <FieldRef Name='{0}' />
                        <Value Type='Choice'>{1}</Value>
                    </Eq>
                    <Eq>
                        <FieldRef Name='{2}' />
                        <Value Type='Text'>{3}</Value>
                    </Eq>
                </And>", SPLists.Issues.Columns.State, state, SPLists.Issues.Columns.TeamId, teamId);
            var query = new CamlQuery();
            query.ViewXml = string.Format("<View><Query><Where>{0}</Where></Query></View>", filter);

            using (var clientContext = await (AuthenticationHelper.GetSharePointClientContextAsync(Permissions.Application)))
            {
                var items = clientContext.GetItems(SPLists.Issues.Title, query);
                var issueArray = items.Select(item => item.ToIssue())
                     .OrderBy(item => item.Id)
                     .ToArray();
                return issueArray;
            }
        }

        public static async Task InsertItemAsync(Issue item)
        {
            using (var clientContext = await (AuthenticationHelper.GetSharePointClientContextAsync(Permissions.Application)))
            {
                var list = clientContext.Web.Lists.GetByTitle(SPLists.Issues.Title); 
                ListItemCreationInformation newItem = new ListItemCreationInformation();
                ListItem listItem = list.AddItem(new ListItemCreationInformation());
                listItem[SPLists.Issues.Columns.Category] = SharePointHelper.BuildSingleLookFieldValue(item.Category.Id,item.Category.Name);
                listItem[SPLists.Issues.Columns.Title] = item.Name;
                listItem[SPLists.Issues.Columns.State] = item.State;
                listItem[SPLists.Issues.Columns.IssueMetric] = item.Metric;
                listItem[SPLists.Issues.Columns.TargetGoal] = item.TargetGoal;
                listItem[SPLists.Issues.Columns.TeamId] = item.MSTeamId;
                listItem.Update();
                clientContext.Load(listItem);
                clientContext.ExecuteQuery();
                item.Id = listItem.Id;
            }
        }

        public static async Task<Issue> GetItemAsync(int id)
        {
            var issue = new Issue();
            using (var clientContext = await (AuthenticationHelper.GetSharePointClientContextAsync(Permissions.Application)))
            {
                var query = new CamlQuery();
                query.ViewXml =
                    @"<View>
                        <Query>
                            <Where>
                                <Eq>
                                    <FieldRef Name='" + SPLists.Issues.Columns.ID + @"'/>
                                    <Value Type='int'>" + id + @"</Value>
                                </Eq>
                            </Where>
                         </Query>
                    </View>";
                var items = clientContext.GetItems(SPLists.Issues.Title, query);
                var queryItem = items.FirstOrDefault();
                if (queryItem == null)
                    return issue;
                issue = queryItem.ToIssue();
                return issue;
            }

        }

        public static async Task UpdateItemAsync(Issue item)
        {
            using (var clientContext = await (AuthenticationHelper.GetSharePointClientContextAsync(Permissions.Application)))
            {
                var query = new CamlQuery();
                query.ViewXml =
                    @"<View>
                        <Query>
                            <Where>
                                <Eq>
                                    <FieldRef Name='" + SPLists.Issues.Columns.ID + @"'/>
                                    <Value Type='int'>" + item.Id + @"</Value>
                                </Eq>
                            </Where>
                         </Query>
                    </View>";
                var items = clientContext.GetItems(SPLists.Issues.Title,query);
                var queryItem = items.FirstOrDefault();
                if (queryItem == null)
                    return;
                queryItem[SPLists.Issues.Columns.State] = item.State;
                queryItem[SPLists.Issues.Columns.IssueMetric] = item.Metric;
                queryItem[SPLists.Issues.Columns.Title] = item.Name;
                queryItem.Update();
                clientContext.ExecuteQuery();
            }
        }
    }
}