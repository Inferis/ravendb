using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Raven.Database.Exceptions;
using Raven.Database.Json;
using Xunit;

namespace Raven.Tests.Patching
{
    public class SimplePatchApplication
    {
        private readonly JObject doc = 
            JObject.Parse(@"{ title: ""A Blog Post"", body: ""html markup"", comments: [ {author: ""ayende"", text:""good post""}] }");

        [Fact]
        public void PropertyAddition()
        {
        	var patchedDoc = new JsonPatcher(doc).Apply(
        		new[]
        		{
        			new PatchRequest
        			{
        				Type = "set",
        				Name = "blog_id",
        				Value = new JValue(1)
        			},
        		});

            Assert.Equal(@"{""title"":""A Blog Post"",""body"":""html markup"",""comments"":[{""author"":""ayende"",""text"":""good post""}],""blog_id"":1}",
                patchedDoc.ToString(Formatting.None));
        }


        [Fact]
        public void PropertyIncrement()
        {
            var patchedDoc = new JsonPatcher(doc).Apply(
                new[]
        		{
        			new PatchRequest
        			{
        				Type = "set",
        				Name = "blog_id",
        				Value = new JValue(1)
        			},
        		});

            patchedDoc = new JsonPatcher(patchedDoc).Apply(
                new[]
        		{
        			new PatchRequest
        			{
        				Type = "inc",
        				Name = "blog_id",
        				Value = new JValue(1)
        			},
        		});

            Assert.Equal(@"{""title"":""A Blog Post"",""body"":""html markup"",""comments"":[{""author"":""ayende"",""text"":""good post""}],""blog_id"":2}",
                patchedDoc.ToString(Formatting.None));
        }

        [Fact]
        public void PropertyAddition_WithConcurrenty_MissingProp()
        {
            var patchedDoc = new JsonPatcher(doc).Apply(
               new[]
        		{
        			new PatchRequest
        			{
        				Type = "set",
        				Name = "blog_id",
        				Value = new JValue(1),
						PrevVal = JObject.Parse("{'a': undefined}").Property("a").Value
        			},
        		});

            Assert.Equal(@"{""title"":""A Blog Post"",""body"":""html markup"",""comments"":[{""author"":""ayende"",""text"":""good post""}],""blog_id"":1}",
                patchedDoc.ToString(Formatting.None));
        }

        [Fact]
        public void PropertyAddition_WithConcurrenty_NullValueOnMissingPropShouldThrow()
        {
            Assert.Throws<ConcurrencyException>(() => new JsonPatcher(doc).Apply(
               new[]
        		{
        			new PatchRequest
        			{
        				Type = "set",
        				Name = "blog_id",
        				Value = new JValue(1),
        				PrevVal = new JValue((object)null)
        			},
        		}));
        }

        [Fact]
        public void PropertyAddition_WithConcurrenty_BadValueOnMissingPropShouldThrow()
        {
            Assert.Throws<ConcurrencyException>(() => new JsonPatcher(doc).Apply(
				new[]
        		{
        			new PatchRequest
        			{
        				Type = "set",
        				Name = "blog_id",
        				Value = new JValue(1),
        				PrevVal =  new JValue(2)
        			},
        		}));
        }

        [Fact]
        public void PropertyAddition_WithConcurrenty_ExistingValueOn_Ok()
        {
            JObject apply = new JsonPatcher(doc).Apply(
                new[]
        		{
        			new PatchRequest
        			{
        				Type = "set",
        				Name = "body",
        				Value = new JValue("differnt markup"),
        				PrevVal = new JValue("html markup")
        			},
        		});

            Assert.Equal(@"{""title"":""A Blog Post"",""body"":""differnt markup"",""comments"":[{""author"":""ayende"",""text"":""good post""}]}", apply.ToString(Formatting.None));
        }


        [Fact]
        public void PropertySet()
        {
            var patchedDoc = new JsonPatcher(doc).Apply(
				 new[]
        		{
        			new PatchRequest
        			{
        				Type = "set",
        				Name = "title",
        				Value = new JValue("another")
        			},
        		});

            Assert.Equal(@"{""title"":""another"",""body"":""html markup"",""comments"":[{""author"":""ayende"",""text"":""good post""}]}", patchedDoc.ToString(Formatting.None));
        }

        [Fact]
        public void PropertySetToNull()
        {
            var patchedDoc = new JsonPatcher(doc).Apply(
				 new[]
        		{
        			new PatchRequest
        			{
        				Type = "set",
        				Name = "title",
        				Value = new JValue((object)null)
        			},
        		});

            Assert.Equal(@"{""title"":null,""body"":""html markup"",""comments"":[{""author"":""ayende"",""text"":""good post""}]}", patchedDoc.ToString(Formatting.None));
        }

        [Fact]
        public void PropertyRemoval()
        {
            var patchedDoc = new JsonPatcher(doc).Apply(
                 new[]
        		{
        			new PatchRequest
        			{
        				Type = "unset",
        				Name = "body",
        			},
        		});

            Assert.Equal(@"{""title"":""A Blog Post"",""comments"":[{""author"":""ayende"",""text"":""good post""}]}", patchedDoc.ToString(Formatting.None));
        }

        [Fact]
        public void PropertyRemoval_WithConcurrency_Ok()
        {
            var patchedDoc = new JsonPatcher(doc).Apply(
				 new[]
        		{
        			new PatchRequest
        			{
        				Type = "unset",
        				Name = "body",
						PrevVal = new JValue("html markup")
        			},
        		});

            Assert.Equal(@"{""title"":""A Blog Post"",""comments"":[{""author"":""ayende"",""text"":""good post""}]}", patchedDoc.ToString(Formatting.None));
        }

        [Fact]
        public void PropertyRemoval_WithConcurrency_OnError()
        {
            Assert.Throws<ConcurrencyException>(() => new JsonPatcher(doc).Apply(
                 new[]
        		{
        			new PatchRequest
        			{
        				Type = "unset",
        				Name = "body",
						PrevVal = new JValue("bad markup")
        			},
        		}));
        }

        [Fact]
        public void PropertyRemovalPropertyDoesNotExists()
        {
            var patchedDoc = new JsonPatcher(doc).Apply(
                new[]
        		{
        			new PatchRequest
        			{
        				Type = "unset",
        				Name = "ip",
        			},
        		});

            Assert.Equal(@"{""title"":""A Blog Post"",""body"":""html markup"",""comments"":[{""author"":""ayende"",""text"":""good post""}]}", patchedDoc.ToString(Formatting.None));
        }
    }
}