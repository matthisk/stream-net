﻿using System;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Threading.Tasks;
using System.Collections.Generic;
using Stream;

namespace stream_net_tests
{
    [TestClass]
    public class IntegrationTests
    {
        private Stream.StreamClient _client;
        private Stream.StreamFeed _user1;
        private Stream.StreamFeed _flat3;
        private Stream.StreamFeed _agg4;
        private Stream.StreamFeed _not5;

        [TestInitialize]
        public void Setup()
        {
            _client = new Stream.StreamClient(
                "98a6bhskrrwj",
                "t3nj7j8m6dtdbbakzbu9p7akjk5da8an5wxwyt6g73nt5hf9yujp8h4jw244r67p",
                new Stream.StreamClientOptions()
                {
                    Location = Stream.StreamApiLocation.USEast
                });
            _user1 = _client.Feed("user", "11");
            _flat3 = _client.Feed("flat", "333");
            _agg4 = _client.Feed("aggregate", "444");
            _not5 = _client.Feed("notification", "555");

            _user1.Delete().Wait();
            _agg4.Delete().Wait();
            _not5.Delete().Wait();
            //System.Threading.Thread.Sleep(3000);
        }

        [TestMethod]
        public async Task TestAddActivity()
        {
            var newActivity = new Stream.Activity("1","test","1");
            var response = await this._user1.AddActivity(newActivity);
            Assert.IsNotNull(response);

            var activities = await this._user1.GetActivities(0, 1);
            Assert.IsNotNull(activities);
            Assert.AreEqual(1, activities.Count());

            var first = activities.First();
            Assert.AreEqual(response.Id, first.Id);
            Assert.AreEqual(response.Actor, first.Actor);
            Assert.AreEqual(response.Object, first.Object);
            Assert.AreEqual(response.Verb, first.Verb);
        }

        [TestMethod]
        public async Task TestAddActivityWithTime()
        {
            var now = DateTime.UtcNow;
            var newActivity = new Stream.Activity("1","test","1")
            {
                Time = now
            };
            var response = await this._user1.AddActivity(newActivity);
            Assert.IsNotNull(response);

            var activities = await this._user1.GetActivities(0, 1);
            Assert.IsNotNull(activities);
            Assert.AreEqual(1, activities.Count());

            var first = activities.First();
            Assert.AreEqual(response.Id, first.Id);

            // using long date string here to skip milliseconds
            //  "now" will have the milliseconds whereas the response or lookup wont
            Assert.AreEqual(now.ToLongDateString(), first.Time.Value.ToLongDateString());
        }

        [TestMethod]
        public async Task TestAddActivityWithArray()
        {
            var newActivity = new Stream.Activity("1", "test", "1");
            newActivity.SetData("complex", new String[] { "tommaso", "thierry", "shawn" });
            var response = await this._user1.AddActivity(newActivity);
            Assert.IsNotNull(response);

            var activities = await this._user1.GetActivities(0, 1);
            Assert.IsNotNull(activities);
            Assert.AreEqual(1, activities.Count());

            var first = activities.First();
            Assert.AreEqual(response.Id, first.Id);

            var complex = first.GetData<String[]>("complex");
            Assert.IsNotNull(complex);
            Assert.AreEqual(3, complex.Length);
            Assert.AreEqual("shawn", complex[2]);
        }

        [TestMethod]
        public async Task TestAddActivityWithString()
        {
            var newActivity = new Stream.Activity("1", "test", "1");
            newActivity.SetData("complex", "string");
            var response = await this._user1.AddActivity(newActivity);
            Assert.IsNotNull(response);

            var activities = await this._user1.GetActivities(0, 1);
            Assert.IsNotNull(activities);
            Assert.AreEqual(1, activities.Count());

            var first = activities.First();
            Assert.AreEqual(response.Id, first.Id);

            var complex = first.GetData<String>("complex");
            Assert.IsNotNull(complex);
            Assert.AreEqual("string", complex);
        }

        [TestMethod]
        public async Task TestAddActivityWithDictionary()
        {
            var dict = new Dictionary<String, String>();
            dict["test1"] = "shawn";
            dict["test2"] = "wedge";

            var newActivity = new Stream.Activity("1", "test", "1");
            newActivity.SetData("complex", dict);
            var response = await this._user1.AddActivity(newActivity);
            Assert.IsNotNull(response);

            var activities = await this._user1.GetActivities(0, 1);
            Assert.IsNotNull(activities);
            Assert.AreEqual(1, activities.Count());

            var first = activities.First();
            Assert.AreEqual(response.Id, first.Id);

            var complex = first.GetData<IDictionary<String,String>>("complex");
            Assert.IsNotNull(complex);
            Assert.AreEqual(2, complex.Count);
            Assert.AreEqual("shawn", complex["test1"]);
        }

        [TestMethod]
        public async Task TestAddActivityTo()
        {
            var newActivity = new Stream.Activity("multi1", "test", "1")
            {
                To = new List<String>("flat:remotefeed1".Yield())
            };
            var addedActivity = await this._user1.AddActivity(newActivity);
            Assert.IsNotNull(addedActivity);
            Assert.IsNotNull(addedActivity.To);
            Assert.AreEqual(1, addedActivity.To.SafeCount());
            Assert.AreEqual("flat:remotefeed1", addedActivity.To.First());

            var activities = await _client.Feed("flat", "remotefeed1").GetActivities(0, 1);            
            Assert.IsNotNull(activities);
            Assert.AreEqual(1, activities.Count());

            var first = activities.First();            
            Assert.AreEqual("multi1", first.Actor);
        }    

        [TestMethod]
        public async Task TestAddActivities()
        {
            var newActivities = new Stream.Activity[] {
                new Stream.Activity("multi1","test","1"),
                new Stream.Activity("multi2","test","2")
            };
            
            var response = await this._user1.AddActivities(newActivities);
            Assert.IsNotNull(response);
            Assert.AreEqual(2, response.Count());

            var activities = await this._user1.GetActivities(0, 2);
            Assert.IsNotNull(activities);
            Assert.AreEqual(2, activities.Count());

            Assert.AreEqual(response.Skip(1).First().Id, activities.First().Id);
            Assert.AreEqual(response.First().Id, activities.Skip(1).First().Id);
        }

        [TestMethod]
        public async Task TestRemoveActivity()
        {
            var newActivity = new Stream.Activity("1", "test", "1");
            var response = await this._user1.AddActivity(newActivity);
            Assert.IsNotNull(response);

            var activities = await this._user1.GetActivities(0, 1);
            Assert.IsNotNull(activities);
            Assert.AreEqual(1, activities.Count());

            var first = activities.FirstOrDefault();
            Assert.AreEqual(response.Id, first.Id);

            await this._user1.RemoveActivity(first.Id);
            var nextActivities = await this._user1.GetActivities(0, 1);
            Assert.IsNotNull(nextActivities);
            Assert.IsFalse(nextActivities.Any(na => na.Id == first.Id));
        }

        [TestMethod]
        public async Task TestRemoveActivityByForeignId()
        {
            var fid = "post:42";
            var newActivity = new Stream.Activity("1", "test", "1")
            {
                ForeignId = fid
            };

            var response = await this._user1.AddActivity(newActivity);
            Assert.IsNotNull(response);
        
            var activities = await this._user1.GetActivities(0, 1);
            Assert.IsNotNull(activities);
            Assert.AreEqual(1, activities.Count());
            Assert.AreEqual(response.Id, activities.First().Id);
            Assert.AreEqual(fid, activities.First().ForeignId);        
        
            await this._user1.RemoveActivity(fid, true);
            activities = await this._user1.GetActivities(0, 1);
            Assert.AreEqual(0, activities.Count());
        }

        [TestMethod]
        public async Task TestDelete() 
        { 
            var newActivity = new Stream.Activity("1","test","1");
            var response = await this._user1.AddActivity(newActivity);
            Assert.IsNotNull(response);

            var activities = await this._user1.GetActivities(0, 1);
            Assert.IsNotNull(activities);
            Assert.AreEqual(1, activities.Count());

            await this._user1.Delete();

            var nextActivities = await this._user1.GetActivities(0, 1);
            Assert.IsNotNull(nextActivities);
            Assert.AreEqual(0, nextActivities.Count());
        } 

        [TestMethod]
        public async Task TestGet()
        {
            var newActivity = new Stream.Activity("1","test","1");
            var first = await this._user1.AddActivity(newActivity);

            newActivity = new Stream.Activity("1","test","2");
            var second = await this._user1.AddActivity(newActivity);

            newActivity = new Stream.Activity("1","test","3");
            var third = await this._user1.AddActivity(newActivity);

            var activities = await this._user1.GetActivities(0, 2);
            Assert.IsNotNull(activities);
            Assert.AreEqual(2, activities.Count());
            Assert.AreEqual(third.Id, activities.First().Id);
            Assert.AreEqual(second.Id, activities.Skip(1).First().Id);

            activities = await this._user1.GetActivities(1, 2);
            Assert.AreEqual(second.Id, activities.First().Id);

            //$id_offset =  ['id_lt' => $third_id];
            activities = await this._user1.GetActivities(0, 2, FeedFilter.Where().IdLessThan(third.Id));
            Assert.AreEqual(second.Id, activities.First().Id);
        }

        [TestMethod]
        public async Task TestGetFlatActivities()
        {
            var newActivity = new Stream.Activity("1", "test", "1");
            var first = await this._user1.AddActivity(newActivity);

            newActivity = new Stream.Activity("1", "test", "2");
            var second = await this._user1.AddActivity(newActivity);

            newActivity = new Stream.Activity("1", "test", "3");
            var third = await this._user1.AddActivity(newActivity);

            var response = await this._user1.GetFlatActivities(GetOptions.Default.WithLimit(2));
            Assert.IsNotNull(response);
            var activities = response.Results;
            Assert.IsNotNull(activities);
            Assert.AreEqual(2, activities.Count());
            Assert.AreEqual(third.Id, activities.First().Id);
            Assert.AreEqual(second.Id, activities.Skip(1).First().Id);

            response = await this._user1.GetFlatActivities(GetOptions.Default.WithOffset(1).WithLimit(2));
            activities = response.Results;
            Assert.AreEqual(second.Id, activities.First().Id);

            response = await this._user1.GetFlatActivities(GetOptions.Default.WithLimit(2).WithFilter(FeedFilter.Where().IdLessThan(third.Id)));
            activities = response.Results;
            Assert.AreEqual(second.Id, activities.First().Id);
        }

        [TestMethod]
        public async Task TestFlatFollowUnfollow()
        {
            this._user1.UnfollowFeed("flat", "333").Wait();
            System.Threading.Thread.Sleep(3000);

            var newActivity = new Stream.Activity("1", "test", "1");
            var response = await this._flat3.AddActivity(newActivity);

            this._user1.FollowFeed("flat", "333").Wait();
            System.Threading.Thread.Sleep(5000);

            var activities = await this._user1.GetActivities(0, 1);
            Assert.IsNotNull(activities);
            Assert.AreEqual(1, activities.Count());
            Assert.AreEqual(response.Id, activities.First().Id);

            this._user1.UnfollowFeed("flat", "333").Wait();
            System.Threading.Thread.Sleep(3000);

            activities = await this._user1.GetActivities(0, 1);
            Assert.IsNotNull(activities);
            Assert.AreEqual(0, activities.Count());
        }

        [TestMethod]
        public async Task TestFlatFollowUnfollowByFeed()
        {
            this._user1.UnfollowFeed(_flat3).Wait();
            System.Threading.Thread.Sleep(3000);

            var newActivity = new Stream.Activity("1", "test", "1");
            var response = await this._flat3.AddActivity(newActivity);

            this._user1.FollowFeed(_flat3).Wait();
            System.Threading.Thread.Sleep(5000);

            var activities = await this._user1.GetActivities(0, 1);
            Assert.IsNotNull(activities);
            Assert.AreEqual(1, activities.Count());
            Assert.AreEqual(response.Id, activities.First().Id);

            this._user1.UnfollowFeed(_flat3).Wait();
            System.Threading.Thread.Sleep(3000);

            activities = await this._user1.GetActivities(0, 1);
            Assert.IsNotNull(activities);
            Assert.AreEqual(0, activities.Count());
        }

        [TestMethod]
        public async Task TestFlatFollowUnfollowPrivate()
        {
            var secret = this._client.Feed("secret", "33");

            this._user1.UnfollowFeed("secret", "33").Wait();
            System.Threading.Thread.Sleep(3000);

            var newActivity = new Stream.Activity("1", "test", "1");
            var response = await secret.AddActivity(newActivity);

            this._user1.FollowFeed("secret", "33").Wait();
            System.Threading.Thread.Sleep(5000);

            var activities = await this._user1.GetActivities(0, 1);
            Assert.IsNotNull(activities);
            Assert.AreEqual(1, activities.Count());
            Assert.AreEqual(response.Id, activities.First().Id);

            await this._user1.UnfollowFeed("secret", "33");
        }

        [TestMethod]
        public async Task TestMarkRead()
        {
            var newActivity = new Stream.Activity("1", "tweet", "1");
            var first = await _not5.AddActivity(newActivity);

            newActivity = new Stream.Activity("1", "run", "2");
            var second = await _not5.AddActivity(newActivity);

            newActivity = new Stream.Activity("1", "share", "3");
            var third = await _not5.AddActivity(newActivity);

            var activities = await _not5.GetActivities(0, 2, marker: ActivityMarker.Mark().AllRead());
            Assert.IsNotNull(activities);
            Assert.AreEqual(2, activities.Count());

            var notActivity = activities.First() as NotificationActivity;
            Assert.IsNotNull(notActivity);
            Assert.IsFalse(notActivity.IsRead);

            notActivity = activities.Skip(1).First() as NotificationActivity;
            Assert.IsNotNull(notActivity);
            Assert.IsFalse(notActivity.IsRead);

            activities = await _not5.GetActivities(0, 2);
            Assert.IsNotNull(activities);
            Assert.AreEqual(2, activities.Count());

            notActivity = activities.First() as NotificationActivity;
            Assert.IsNotNull(notActivity);
            Assert.IsTrue(notActivity.IsRead);

            notActivity = activities.Skip(1).First() as NotificationActivity;
            Assert.IsNotNull(notActivity);
            Assert.IsTrue(notActivity.IsRead);
        }

        [TestMethod]
        public async Task TestMarkReadByIds()
        {
            var newActivity = new Stream.Activity("1", "tweet", "1");
            var first = await _not5.AddActivity(newActivity);

            newActivity = new Stream.Activity("1", "run", "2");
            var second = await _not5.AddActivity(newActivity);

            newActivity = new Stream.Activity("1", "share", "3");
            var third = await _not5.AddActivity(newActivity);

            var activities = await _not5.GetActivities(0, 2);

            var marker = ActivityMarker.Mark();
            foreach (var activity in activities)
            {
                marker = marker.Read(activity.Id);
            }            

            var notActivity = activities.First() as NotificationActivity;
            Assert.IsNotNull(notActivity);
            Assert.IsFalse(notActivity.IsRead);

            notActivity = activities.Skip(1).First() as NotificationActivity;
            Assert.IsNotNull(notActivity);
            Assert.IsFalse(notActivity.IsRead);

            activities = await _not5.GetActivities(0, 3, marker: marker);

            activities = await _not5.GetActivities(0, 3);
            Assert.IsNotNull(activities);
            Assert.AreEqual(3, activities.Count());

            notActivity = activities.First() as NotificationActivity;
            Assert.IsNotNull(notActivity);
            Assert.IsTrue(notActivity.IsRead);

            notActivity = activities.Skip(1).First() as NotificationActivity;
            Assert.IsNotNull(notActivity);
            Assert.IsTrue(notActivity.IsRead);

            notActivity = activities.Skip(2).First() as NotificationActivity;
            Assert.IsNotNull(notActivity);
            Assert.IsFalse(notActivity.IsRead);
        }

        [TestMethod]
        public async Task TestMarkNotificationsRead()
        {
            var newActivity = new Stream.Activity("1", "tweet", "1");
            var first = await _not5.AddActivity(newActivity);

            newActivity = new Stream.Activity("1", "run", "2");
            var second = await _not5.AddActivity(newActivity);

            newActivity = new Stream.Activity("1", "share", "3");
            var third = await _not5.AddActivity(newActivity);

            var response = await _not5.GetNotificationActivities(GetOptions.Default.WithLimit(2).WithMarker(ActivityMarker.Mark().AllRead()));
            Assert.IsNotNull(response);
            Assert.AreEqual(3, response.Unseen);

            var activities = response.Results;
            Assert.IsNotNull(activities);
            Assert.AreEqual(2, activities.Count());

            var notActivity = activities.First();
            Assert.IsNotNull(notActivity);
            Assert.IsFalse(notActivity.IsRead);

            notActivity = activities.Skip(1).First();
            Assert.IsNotNull(notActivity);
            Assert.IsFalse(notActivity.IsRead);

            response = await _not5.GetNotificationActivities(GetOptions.Default.WithLimit(2));

            Assert.IsNotNull(response);
            Assert.AreEqual(0, response.Unread);
            Assert.AreEqual(3, response.Unseen);

            activities = response.Results;
            Assert.IsNotNull(activities);
            Assert.AreEqual(2, activities.Count());

            notActivity = activities.First();
            Assert.IsNotNull(notActivity);
            Assert.IsTrue(notActivity.IsRead);

            notActivity = activities.Skip(1).First();
            Assert.IsNotNull(notActivity);
            Assert.IsTrue(notActivity.IsRead);
        }

        [TestMethod]
        public async Task TestFollowersEmpty()
        {
            var lonely = this._client.Feed("flat", "lonely");
            var response = await lonely.Followers();
            Assert.IsNotNull(response);
            Assert.AreEqual(0, response.Count());
        }

        [TestMethod]
        public async Task TestFollowersWithLimit()
        {
            this._client.Feed("flat", "csharp43").FollowFeed("flat", "csharp42").Wait();
            this._client.Feed("flat", "csharp44").FollowFeed("flat", "csharp42").Wait();
            var response = await this._client.Feed("flat", "csharp42").Followers(0, 2);
            Assert.IsNotNull(response);
            Assert.AreEqual(2, response.Count());
            Assert.AreEqual(response.First().FeedId, "flat:csharp44");
            Assert.AreEqual(response.First().TargetId, "flat:csharp42");
        }

        [TestMethod]
        public async Task TestFollowingEmpty()
        {
            var lonely = this._client.Feed("flat", "lonely");
            var response = await lonely.Following();
            Assert.IsNotNull(response);
            Assert.AreEqual(0, response.Count());
        }

        [TestMethod]
        public async Task TestFollowingsWithLimit()
        {
            this._client.Feed("flat", "csharp43").FollowFeed("flat", "csharp42").Wait();
            this._client.Feed("flat", "csharp43").FollowFeed("flat", "csharp44").Wait();
            var response = await this._client.Feed("flat", "csharp43").Following(0, 2);
            Assert.IsNotNull(response);
            Assert.AreEqual(2, response.Count());
            Assert.AreEqual(response.First().FeedId, "flat:csharp43");
            Assert.AreEqual(response.First().TargetId, "flat:csharp44");
        }

        [TestMethod]
        public async Task TestDoIFollowEmpty()
        {
            var lonely = this._client.Feed("flat", "lonely");
            var response = await lonely.Following(0, 10, new String[] { "flat:asocial" });
            Assert.IsNotNull(response);
            Assert.AreEqual(0, response.Count());
        }

        [TestMethod]
        public async Task TestAggregate()
        {
            var newActivity1 = new Stream.Activity("1", "test", "1");
            var newActivity2 = new Stream.Activity("1", "test", "2");
            var response = await _user1.AddActivity(newActivity1);
            response = await _user1.AddActivity(newActivity2);

            System.Threading.Thread.Sleep(3000);

            await _agg4.FollowFeed("user", "11");
            System.Threading.Thread.Sleep(3000);

            var activities = await this._agg4.GetActivities(0);
            Assert.IsNotNull(activities);
            Assert.AreEqual(1, activities.Count());

            var aggActivity = activities.First() as AggregateActivity;
            Assert.IsNotNull(aggActivity);
            Assert.AreEqual(2, aggActivity.Activities.Count);
            Assert.AreEqual(1, aggActivity.ActorCount);

            await _agg4.UnfollowFeed("user", "11");
        }

        [TestMethod]
        public async Task TestGetAggregate()
        {
            var newActivity1 = new Stream.Activity("1", "test", "1");
            var newActivity2 = new Stream.Activity("1", "test", "2");
            var response = await _user1.AddActivity(newActivity1);
            response = await _user1.AddActivity(newActivity2);

            System.Threading.Thread.Sleep(3000);

            await _agg4.FollowFeed("user", "11");
            System.Threading.Thread.Sleep(3000);

            var result = await this._agg4.GetAggregateActivities();
            var activities = result.Results;
            Assert.IsNotNull(activities);
            Assert.AreEqual(1, activities.Count());

            var aggActivity = activities.First();
            Assert.IsNotNull(aggActivity);
            Assert.AreEqual(2, aggActivity.Activities.Count);
            Assert.AreEqual(1, aggActivity.ActorCount);

            await _agg4.UnfollowFeed("user", "11");
        }

        [TestMethod]
        public async Task TestMixedAggregate()
        {
            var newActivity1 = new Stream.Activity("1", "test", "1");
            var newActivity2 = new Stream.Activity("1", "test", "2");
            var newActivity3 = new Stream.Activity("1", "other", "2");
            var response = await _user1.AddActivity(newActivity1);
            response = await _user1.AddActivity(newActivity2);
            response = await _user1.AddActivity(newActivity3);

            System.Threading.Thread.Sleep(3000);

            await _agg4.FollowFeed("user", "11");
            System.Threading.Thread.Sleep(3000);

            var activities = await this._agg4.GetActivities(0);
            Assert.IsNotNull(activities);
            Assert.AreEqual(2, activities.Count());

            var aggActivity = activities.First() as AggregateActivity;
            Assert.IsNotNull(aggActivity);
            Assert.AreEqual(1, aggActivity.Activities.Count);
            Assert.AreEqual(1, aggActivity.ActorCount);

            await _agg4.UnfollowFeed("user", "11");
        }
    }
}
