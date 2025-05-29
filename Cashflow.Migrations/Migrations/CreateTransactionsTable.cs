using FluentMigrator;

[Migration(2024052901)]
public class CreateTransactionsTable : Migration
{
    public override void Up()
    {
        Create.Table("transactions")
            .WithColumn("id").AsGuid().PrimaryKey()
            .WithColumn("amount").AsDecimal(18, 2).NotNullable()
            .WithColumn("type").AsInt32().NotNullable()
            .WithColumn("timestamp").AsDateTimeOffset().NotNullable()
            .WithColumn("id_potency_key").AsGuid().NotNullable().Unique();
    }

    public override void Down()
    {
        Delete.Table("transactions");
    }
}