﻿using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Data.Entity;

namespace Eddard
{

    public class DocumentsContext : DbContext
    {
        public DocumentsContext()
            : base("DocumentsContext")
        {

        }
        public DbSet<Repository> Repositories { get; set; }
        public DbSet<Document> Documents { get; set; }
        public DbSet<Property> Properties { get; set; }
        public DbSet<PropValue> PropValues { get; set; }
        public DbSet<File> Files { get; set; }
    }

    public class Repository
    {
        [Key]
        public Guid Guid { get; set; }

        [Required]
        [StringLength(255)]
        [Index(IsUnique = true)]
        public string Name { get; set; }
    }

    public class ObjectWithProperties
    {
        public virtual ICollection<PropValue> Properties { get; set; }

        public ObjectWithProperties()
        {
            Properties = new HashSet<PropValue>();
        }
    }

    
    public class PropValue
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int PropertyValueId { get; set; }

        [Required]
        public virtual Property Property { get; set; }
        [Required]
        public string Value { get; set; }


        // public ICollection<Document> Publications { get; set; }
        public ICollection<Document> Documents { get; set; }
    }

    public class Property
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int PropertyId { get; set; }

        [Index(IsUnique = true)]
        [StringLength(255)]
        [Required]
        public string Name { get; set; }

        public ICollection<PropValue> Values { get; set; }
    }



    public class Document : ObjectWithProperties
    {

        public virtual ICollection<File> Files { get; set; }

        [Key]
        public Guid DocumentId { get; set; }

        [Required]
        public virtual Repository Repository { get; set; }

        //[Index("LangAndDocIndex", 1)]
        //[Required]
        //public Language Language { get; set; }

        //[Index("LangAndDocIndex", 2, IsUnique = true)]
        //[Required]
        //public Document Document { get; set; }

        public DateTime? PubDate { get; set; }

        [Required]
        public string NameOrTitle { get; set; }

        [Required]
        public DateTime Created { get; set; }

        [Required]
        public DateTime LastModified { get; set; }

        public Document()
        {
            Files = new HashSet<File>();
        }
    }

    public class File
    {

        [Key]
        public Guid Guid { get; set; }

        [Required]
        public Document Document { get; set; }
        
        [Required]
        [StringLength(5)]
        public string Extension { get; set; }
        [Required]
        public long Size { get; set; }

    }
}
