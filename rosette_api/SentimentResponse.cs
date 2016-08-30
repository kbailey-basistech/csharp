﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.Script.Serialization;
using System.Net.Http;

namespace rosette_api
{
    /// <summary>
    /// A class for representing responses from the sentiment analysis endpoint of the Rosette API
    /// </summary>
    public class SentimentResponse : RosetteResponse
    {
        private const string docKey = "document";
        private const string entitiesKey = "entities";
        private const string labelKey = "label";
        private const string confidenceKey = "confidence";
        private const string mentionKey = "mention";
        private const string normalizedMentionKey = "normalized";
        private const string countKey = "count";
        private const string typeKey = "type";
        private const string entityIDKey = "entityId";
        private const string sentimentKey = "sentiment";

        /// <summary>
        /// Gets or sets the document-level sentiment identified by the Rosette API
        /// </summary>
        public RosetteSentiment DocSentiment { get; set; }
        /// <summary>
        /// Gets or sets the entities identified by the Rosette API with sentiment
        /// </summary>
        public List<RosetteSentimentEntity> EntitySentiments { get; set; }

        /// <summary>
        /// Gets the response headers returned from the API
        /// </summary>
        public ResponseHeaders ResponseHeaders { get; private set; }

        /// <summary>
        /// Creates a SentimentResponse from the given apiResult
        /// </summary>
        /// <param name="apiResult">The message from the API</param>
        public SentimentResponse(HttpResponseMessage apiResult)
            : base(apiResult)
        {
            List<RosetteSentimentEntity> entitySentiments = new List<RosetteSentimentEntity>();
            Dictionary<string, object> docResult = this.Content.ContainsKey(docKey) ? this.Content[docKey] as Dictionary<string, object> : new Dictionary<string, object>();
            if (docResult.ContainsKey(labelKey) && docResult.ContainsKey(confidenceKey))
            {
                this.DocSentiment = new RosetteSentiment(docResult[labelKey] as String, docResult[confidenceKey] as Nullable<decimal>);
            }
            IEnumerable<Object> enumerableResults = this.Content.ContainsKey(entitiesKey) ? this.Content[entitiesKey] as IEnumerable<Object> : new List<object>();
            foreach (Object result in enumerableResults)
            {
                Dictionary<string, object> dictResult = result as Dictionary<string, object>;
                String type = dictResult.ContainsKey(typeKey) ? (dictResult[typeKey] as String) : null;
                String mention = dictResult.ContainsKey(mentionKey) ? dictResult[mentionKey] as String : null;
                String normalizedMention = dictResult.ContainsKey(normalizedMentionKey) ? dictResult[normalizedMentionKey] as String : null;
                String entityIDStr = dictResult.ContainsKey(entityIDKey) ? dictResult[entityIDKey] as String : null;
                EntityID entityID = entityIDStr != null ? new EntityID(entityIDStr) : null;
                Nullable<int> count = dictResult.ContainsKey(countKey) ? dictResult[countKey] as Nullable<int> : null;
                String sentiment = null;
                Nullable<decimal> confidence = null;
                if (dictResult.ContainsKey(sentimentKey))
                {
                    Dictionary<string, object> sentimentDict = dictResult[sentimentKey] as Dictionary<string, object>;
                    sentiment = sentimentDict.ContainsKey(labelKey) ? sentimentDict[labelKey] as String : null;
                    confidence = sentimentDict.ContainsKey(confidenceKey) ? sentimentDict[confidenceKey] as Nullable<decimal> : null;
                }
                entitySentiments.Add(new RosetteSentimentEntity(mention, normalizedMention, entityID, type, count, sentiment, confidence));
            }
            this.EntitySentiments = entitySentiments;
            this.ResponseHeaders = new ResponseHeaders(this.Headers);
        }

        /// <summary>
        /// Creates a SentimentResponse from its components
        /// </summary>
        /// <param name="docSentiment">The sentiment of the entire document/input text</param>
        /// <param name="entitySentiments">The entities, each with a sentiment, found in the input text</param>
        /// <param name="responseHeaders">The response headers returned from the API</param>
        /// <param name="content">The content (the doc and entity sentiments) in Dictionary form</param>
        /// <param name="contentAsJson">The content in JSON form</param>
        public SentimentResponse(RosetteSentiment docSentiment, List<RosetteSentimentEntity> entitySentiments, Dictionary<string, string> responseHeaders, Dictionary<string, object> content, string contentAsJson)
            : base(responseHeaders, content, contentAsJson)
        {
            this.DocSentiment = docSentiment;
            this.EntitySentiments = entitySentiments;
            this.ResponseHeaders = new ResponseHeaders(responseHeaders);
        }

        /// <summary>
        /// Equals override
        /// </summary>
        /// <param name="obj">The object to compare against</param>
        /// <returns>True if equal</returns>
        public override bool Equals(object obj)
        {
            if (obj is SentimentResponse)
            {
                SentimentResponse other = obj as SentimentResponse;
                List<bool> conditions = new List<bool>() {
                    this.DocSentiment != null && other.DocSentiment != null ? this.DocSentiment.Equals(other.DocSentiment) : this.DocSentiment == other.DocSentiment,
                    this.EntitySentiments != null && other.EntitySentiments != null ? this.EntitySentiments.SequenceEqual(other.EntitySentiments) : this.EntitySentiments == other.EntitySentiments,
                    this.ResponseHeaders != null && other.ResponseHeaders != null ? this.ResponseHeaders.Equals(other.ResponseHeaders) : this.ResponseHeaders == other.ResponseHeaders,
                    this.GetHashCode() == other.GetHashCode()
                };
                return conditions.All(condition => condition);
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// Hashcode override
        /// </summary>
        /// <returns>The hashcode</returns>
        public override int GetHashCode()
        {
            int h0 = this.ResponseHeaders != null ? this.ResponseHeaders.GetHashCode() : 1;
            int h1 = this.EntitySentiments != null ? this.EntitySentiments.Aggregate<RosetteSentimentEntity, int>(1, (seed, item) => seed ^ item.GetHashCode()) : 1;
            int h2 = this.DocSentiment != null ? this.DocSentiment.GetHashCode() : 1;
            return h0 ^ h1 ^ h2;
        }

        /// <summary>
        /// ToString override.
        /// </summary>
        /// <returns>This SentimentResponse in JSON form</returns>
        public override string ToString()
        {
            StringBuilder builder = new StringBuilder();
            builder.Append("{\"document\": ").Append(this.DocSentiment.ToString()).Append(", ")
                .Append("\"entities\": [").Append(String.Join(", ", this.EntitySentiments)).Append("]")
                .Append(", responseHeaders: ").Append(this.ResponseHeaders.ToString()).Append("}");
            return builder.ToString();
        }

        /// <summary>
        /// Gets the content in JSON form
        /// </summary>
        /// <returns>The content in JSON form</returns>
        public string ContentToString()
        {
            StringBuilder builder = new StringBuilder();
            builder.Append("{\"document\": ").Append(this.DocSentiment.ToString()).Append(", ")
                .Append("\"entities\": [").Append(String.Join(", ", this.EntitySentiments)).Append("]}");
            return builder.ToString();
        }

        /// <summary>
        /// A class for representing sentiments
        /// </summary>
        public class RosetteSentiment
        {
            /// <summary>
            /// The enumeration of possible sentiment labels
            /// </summary>
            public enum SentimentLabel
            {
                /// <summary>
                /// The sentiment is positive
                /// </summary>
                pos,
                /// <summary>
                /// The sentiment is neutral
                /// </summary>
                /// 
                neu,
                /// <summary>
                /// The sentiment is negative
                /// </summary>
                neg
            };
            /// <summary>
            /// On a scale of 0-1, the confidence in the Label's correctness.
            /// </summary>
            public Nullable<decimal> Confidence;
            /// <summary>
            /// The label indicating the sentiment
            /// </summary>
            public SentimentLabel Label;

            /// <summary>
            /// A constructor for creating RosetteSentiments
            /// </summary>
            /// <param name="sentiment">The sentiment label: "pos", "neu", or "neg"</param>
            /// <param name="confidence">An indicator of confidence in the label being correct.  A range from 0-1.</param>
            public RosetteSentiment(String sentiment, Nullable<decimal> confidence)
            {
                switch (sentiment)
                {
                    case "pos": this.Label = SentimentLabel.pos; break;
                    case "neu": this.Label = SentimentLabel.neu; break;
                    case "neg": this.Label = SentimentLabel.neg; break;
                    default: throw new ArgumentException("The sentiment label returned by the API has been changed.  The binding needs to be updated.", "sentiment");
                }
                this.Confidence = confidence;
            }

            /// <summary>
            /// Equals override
            /// </summary>
            /// <param name="obj">The object to compare against</param>
            /// <returns>True if equal</returns>
            public override bool Equals(object obj)
            {
                if (obj is RosetteSentiment)
                {
                    RosetteSentiment other = obj as RosetteSentiment;
                    List<bool> conditions = new List<bool>() {
                        this.Confidence == other.Confidence,
                        this.Label.Equals(other.Label),
                        this.GetHashCode() == other.GetHashCode()
                    };
                    return conditions.All(condition => condition);
                }
                else
                {
                    return false;
                }
            }

            /// <summary>
            /// HashCode override
            /// </summary>
            /// <returns>The hashcode</returns>
            public override int GetHashCode()
            {
                int h0 = this.Confidence != null ? this.Confidence.GetHashCode() : 1;
                int h1 = this.Label.GetHashCode();
                return h0 ^ h1;
            }

            /// <summary>
            /// ToString override.
            /// </summary>
            /// <returns>Writes this RosetteSentiment in JSON form</returns>
            public override string ToString()
            {
                StringBuilder builder = new StringBuilder();
                builder.Append("{\"label\": \"").Append(this.Label.ToString()).Append("\", ")
                    .Append("\"confidence\": ").Append(this.Confidence.ToString()).Append("}");
                return builder.ToString();
            }
        }
    }

    /// <summary>
    /// A class to represent an entity returned by the Sentiment Analysis endpoint of the Rosette API
    /// </summary>
    public class RosetteSentimentEntity : RosetteEntity
    {
        /// <summary>
        /// The sentiment label of this entity
        /// </summary>
        public SentimentResponse.RosetteSentiment Sentiment { get; set; }

        /// <summary>
        /// Creates an entity that has a sentiment associated with it
        /// </summary>
        /// <param name="mention">The mention of the entity</param>
        /// <param name="normalizedMention">The normalized mention of the entity</param>
        /// <param name="id">The contextual ID of the entity to compare it against other entities</param>
        /// <param name="entityType">The entity type</param>
        /// <param name="count">The number of times the entity appeared in the text</param>
        /// <param name="sentiment">The contextual sentiment of the entity</param>
        /// <param name="confidence">The confidence that the sentiment was correctly identified</param>
        public RosetteSentimentEntity(String mention, String normalizedMention, EntityID id, String entityType, Nullable<int> count, String sentiment, Nullable<decimal> confidence) : base(mention, normalizedMention, id, entityType, count)
        {
            this.Sentiment = new SentimentResponse.RosetteSentiment(sentiment, confidence);
        }

        /// <summary>
        /// Is this RosetteSentimentEntity the same as the other entity?
        /// </summary>
        /// <param name="other">The other entity</param>
        /// <returns>True if the entities are equal (sentiment is ignored).</returns>
        public bool EntityEquals(RosetteEntity other)
        {
           List<bool> conditions = new List<bool>() {
               this.Mention == other.Mention &&
               this.NormalizedMention != null && other.NormalizedMention != null ? this.NormalizedMention.Equals(other.NormalizedMention) : this.NormalizedMention == other.NormalizedMention,
               this.Mention != null && other.Mention != null ? this.Mention.Equals(other.Mention) : this.Mention == other.Mention,
               this.ID != null && other.ID != null ? this.ID.Equals(other.ID) : this.ID == other.ID,
               this.EntityType != null && other.EntityType != null ? this.EntityType.Equals(other.EntityType) : this.EntityType == other.EntityType,
               this.Count != null && other.Count != null ? this.Count.Equals(other.Count) : this.Count == other.Count
           };
           return conditions.All(condition => condition);
        }

        /// <summary>
        /// Equals override
        /// </summary>
        /// <param name="obj">The object to compare against</param>
        /// <returns>True if equal</returns>
        public override bool Equals(object obj)
        {
            if (obj is RosetteSentimentEntity)
            {
                RosetteSentimentEntity other = obj as RosetteSentimentEntity;
                List<bool> conditions = new List<bool>() {
                    this.Sentiment != null && other.Sentiment != null ? this.Sentiment.Equals(other.Sentiment) : this.Sentiment == other.Sentiment,
                    this.EntityEquals(other),
                    this.GetHashCode() == other.GetHashCode()
                };
                return conditions.All(condition => condition);
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// HashCode override
        /// </summary>
        /// <returns>The hashcode</returns>
        public override int GetHashCode()
        {
            int h0 = this.Count != null ? this.Count.GetHashCode() : 1;
            int h1 = this.EntityType != null ? this.EntityType.GetHashCode() : 1;
            int h2 = this.ID != null ? this.ID.GetHashCode() : 1;
            int h3 = this.Mention != null ? this.Mention.GetHashCode() : 1;
            int h4 = this.NormalizedMention != null ? this.NormalizedMention.GetHashCode() : 1;
            int h5 = this.Sentiment != null ? this.Sentiment.GetHashCode() : 1;
            return h0 ^ h1 ^ h2 ^ h3 ^ h4 ^ h5;
        }

        /// <summary>
        /// ToString override.
        /// </summary>
        /// <returns>This RosetteSentimentEntity in JSON form</returns>
        public override string ToString()
        {
            StringBuilder builder = new StringBuilder();
            builder.Append("{\"type\": \"").Append(this.EntityType).Append("\", ")
                .Append("\"mention\": \"").Append(this.Mention).Append("\", ")
                .Append("\"normalized\": \"").Append(this.NormalizedMention).Append("\", ")
                .Append("\"count\": ").Append(this.Count.ToString()).Append(", ")
                .Append("\"entityId\": \"").Append(this.ID.ToString()).Append("\", ")
                .Append("\"sentiment\": ").Append(this.Sentiment.ToString()).Append("}");
            return builder.ToString();
        }
    }
}
