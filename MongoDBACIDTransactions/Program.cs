using System;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Bson.Serialization.Attributes;

namespace MongoDBTransaction
{
    class Program
    {
        public class Product
        {
            [BsonId]
            public ObjectId Id { get; set; }
            [BsonElement("SKU")]
            public int SKU { get; set; }
            [BsonElement("Description")]
            public string Description { get; set; }
            [BsonElement("Price")]
            public Double Price { get; set; }
        }

        const string MongoDBConnectionString = "mongodb+srv://<<MONGODB v4 CONNECTION STRING>>";

        static async Task Main(string[] args)
        {
            if (!await UpdateProducts()) { Environment.Exit(0); }
            Console.WriteLine("Finished updating the product collection");
            Console.ReadKey();
        }
        static async Task<bool> UpdateProducts()
        {
            //Create client connection to our MongoDB database
            var client = new MongoClient(MongoDBConnectionString);

            //Create a session object that is used when leveraging transactions
            var session = client.StartSession();

            //Create the collection object that represents the "products" collection
            var products = session.Client.GetDatabase("MongoDBStore").GetCollection<Product>("products");

            //Clean up the collection if there is data in there
            products.Database.DropCollection("products");

            //Create some sample data
            var TV = new Product { Description = "Television", SKU = 4001, Price = 2000 };
            var Book = new Product { Description = "A funny book", SKU = 43221, Price = 19.99 };
            var DogBowl = new Product { Description = "Bowl for Fido", SKU = 123, Price = 40.00 };

            //Begin transaction
            session.StartTransaction();

            try
            {
                //Insert the sample data 
                await products.InsertOneAsync(TV);
                await products.InsertOneAsync(Book);
                await products.InsertOneAsync(DogBowl);

                var filter = new FilterDefinitionBuilder<Product>().Empty;
                var results = await products.Find<Product>(filter).ToListAsync();
                Console.WriteLine("Original Prices:\n");
                foreach (Product d in results)
                {
                    Console.WriteLine(String.Format("Product Name: {0}\tPrice: {1:0.00}", d.Description, d.Price));
                }

                //Increase all the prices by 10% for all products
                var update = new UpdateDefinitionBuilder<Product>().Mul<Double>(r => r.Price, 1.1);
                await products.UpdateManyAsync(filter, update); //,options);

                //Made it here without error? Let's commit the transaction
                session.CommitTransaction();

                //Let's print the new results to the console
                Console.WriteLine("Original Prices:\n");
                results = await products.Find<Product>(filter).ToListAsync();
                foreach (Product d in results)
                {
                    Console.WriteLine(String.Format("Product Name: {0}\tPrice: {1:0.00}", d.Description, d.Price));
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("Error writing to MongoDB: " + e.Message);
                session.AbortTransaction();
            }
            return true;
        }
    }
}