namespace DaugmanIris.Model
{
    using System;
    using System.ComponentModel.DataAnnotations;
    using System.Data.Entity;
    using System.Drawing.Imaging;
    using System.IO;
    using System.Linq;

    public class ImageContext : DbContext
    {
        // Your context has been configured to use a 'Image' connection string from your application's 
        // configuration file (App.config or Web.config). By default, this connection string targets the 
        // 'DaugmanIris.Model.Image' database on your LocalDb instance. 
        // 
        // If you wish to target a different database and/or database provider, modify the 'Image' 
        // connection string in the application configuration file.
        public ImageContext()
            : base("name=ImageContext")
        {
        }

        // Add a DbSet for each entity type that you want to include in your model. For more information 
        // on configuring and using a Code First model, see http://go.microsoft.com/fwlink/?LinkId=390109.

        // public virtual DbSet<MyEntity> MyEntities { get; set; }
        public virtual DbSet<MyImage> MyImages { get; set; }
    }

    public class MyImage
    {
        [Key] public string Name { get; set; }
        public byte[] Image { get; set; }
        public int IrisX { get; set; }
        public int IrisY { get; set; }
        public int IrisR { get; set; }
        public int PupilX { get; set; }
        public int PupilY { get; set; }
        public int PupilR { get; set; }
        public int[] FVector { get; set; }

        public System.Drawing.Image GetPicture()
        {
            MemoryStream ms = new MemoryStream(Image);
            System.Drawing.Image returnImage = System.Drawing.Image.FromStream(ms);
            return returnImage;
        }

        public void SavePicture(System.Drawing.Image imageIn)
        {
            Image = ImageToByteArray(imageIn);
        }

        private byte[] ImageToByteArray(System.Drawing.Image imageIn)
        {
            using (var ms = new MemoryStream())
            {
                imageIn.Save(ms, ImageFormat.Png);
                return ms.ToArray();
            }
        }
    }
}