namespace TekConf.Mobile.Core.Dtos
{
	using System;
	using System.Collections.Generic;

	public class Address
	{
		public int StreetNumber { get; set; }
		public string StreetName { get; set; }
		public string City { get; set; }
		public string State { get; set; }
		public string PostalArea { get; set; }
		public string Country { get; set; }
	}
}