# WeSupplyWorker


WeSupply API Documentation
--------------------------
https://documenter.getpostman.com/view/11859344/T17AiAYq


WeSupply Sample API Request / Postman
-------------------------------------
https://www.getpostman.com/collections/0f9fda38cbdf5946c61a


Objective
---------

Batch process to download return information from wesupply for a given company.

Return information will be stored in:


ShippersRMAAuth        (Header)
ShippersRMAAuthEntry   (Detail)


Steps:
------

1 Obtain Token
https://thecloseout.labs.wesupply.xyz/api/oauth/token

2 Obtain Returns (API returns info per pages 50 each page, we need ideas not download same info, again and again)
https://thecloseout.labs.wesupply.xyz/api/returns?page=1

3 Loop through the List / Pages 

4 Insert missing returns in DB (Hdr, Dtl).

Note: API to return a single return does not include item sku making it not usable for our objective
----------------------------------------------------------------------------------------------------


DB Details
----------

    ShippersRMAAuth:

	[ID]            	
	[CompanyId]         Obtain from appsettings.json
	[RMANumber]         Obtain from JSON return_number
	[OrderNumber]       Obiain from JSON order.number_ext
	[TrackingNumber]    Obtain from JSON logistics.return_shipping_info.tracking
    [Carrier]           Obtain from JSON logistics.return_shipping_info.return_carrier
	[CreatedDate]       GETDATE()
	[Status]            'Pending'
	[JSON]              JSON String

    ShippersRMAAuthEntry:

	[ID] 
	[CompanyId]         Obtain from appsettings.json
	[RMAAuthID]         ID from Hdr record
	[ItemNumber]        Obtain from JSON items[].sku
	[Name]				Obtain from JSON items[].name
    [Quantity]			1
	[Reason] 			items[].return_reason,
	[ReStockCode]		null
	[Comment]			Obtain from JSON  return_comment
	[UserName]			null
	[UpdatedDate]		null

	/****** Script for SelectTopNRows command from SSMS  ******/
	SELECT TOP (1000) [ID]
		,[OptionKey]
		,[OptionValue]
		,[UserModifiable]
	FROM [Merlin].[dbo].[eCommerceMerlinOptions]


	WESUPPLY_LAST_PAGE