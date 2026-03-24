using System;
using System.Text.Json.Serialization;

namespace Projektsoftware.Models
{
    public class EasybillCustomer
    {
        [JsonPropertyName("id")]
        public long Id { get; set; }

        [JsonPropertyName("number")]
        public string? Number { get; set; }

        [JsonPropertyName("company_name")]
        public string? CompanyName { get; set; }

        [JsonPropertyName("first_name")]
        public string? FirstName { get; set; }

        [JsonPropertyName("last_name")]
        public string? LastName { get; set; }

        [JsonPropertyName("emails")]
        public string[]? Emails { get; set; }

        [JsonPropertyName("phone_1")]
        public string? Phone1 { get; set; }

        [JsonPropertyName("phone_2")]
        public string? Phone2 { get; set; }

        [JsonPropertyName("street")]
        public string? Street { get; set; }

        [JsonPropertyName("zip_code")]
        public string? Zipcode { get; set; }

        [JsonPropertyName("city")]
        public string? City { get; set; }

        [JsonPropertyName("country")]
        public string? Country { get; set; }

        [JsonPropertyName("state")]
        public string? State { get; set; }

        [JsonPropertyName("vat_identifier")]
        public string? VatId { get; set; }

        [JsonPropertyName("note")]
        public string? Note { get; set; }

        [JsonPropertyName("created_at")]
        public string? CreatedAt { get; set; }

        [JsonPropertyName("updated_at")]
        public string? UpdatedAt { get; set; }

        // Weitere wichtige Felder aus der API
        [JsonPropertyName("login_id")]
        public long? LoginId { get; set; }

        [JsonPropertyName("display_name")]
        public string? DisplayNameApi { get; set; }

        [JsonPropertyName("archived")]
        public bool Archived { get; set; }

        [JsonPropertyName("personal")]
        public bool Personal { get; set; }

        [JsonPropertyName("salutation")]
        public int Salutation { get; set; }

        [JsonPropertyName("title")]
        public string? Title { get; set; }

        [JsonPropertyName("mobile")]
        public string? Mobile { get; set; }

        [JsonPropertyName("fax")]
        public string? Fax { get; set; }

        [JsonPropertyName("internet")]
        public string? Internet { get; set; }

        // Display properties
        public string DisplayName => DisplayNameApi 
            ?? (!string.IsNullOrEmpty(CompanyName) 
                ? CompanyName 
                : $"{FirstName} {LastName}".Trim());

        public string Email => Emails?.Length > 0 ? Emails[0] : "";

        public string FullAddress => $"{Street}, {Zipcode} {City}, {Country}".Trim();
    }

    public class EasybillCustomerList
    {
        [JsonPropertyName("page")]
        public int Page { get; set; }

        [JsonPropertyName("pages")]
        public int Pages { get; set; }

        [JsonPropertyName("limit")]
        public int Limit { get; set; }

        [JsonPropertyName("total")]
        public int Total { get; set; }

        [JsonPropertyName("items")]
        public EasybillCustomer[]? Items { get; set; }
    }
}
