# WeSupplyWorker


WeSupply API Socumentation
https://documenter.getpostman.com/view/11859344/T17AiAYq#30ca0a2d-df7e-486e-8f47-6776e978ce6a


WeSupply Sample API Request / Postman

https://www.getpostman.com/collections/0f9fda38cbdf5946c61a


Objective
=========

Batch process to download return information from wesupply for a given company.

Return information will be stored in:


ShippersRMAAuth        (Header)
ShippersRMAAuthEntry   (Detail)


Steps:
======

1 Obtain Token
https://thecloseout.labs.wesupply.xyz/api/oauth/token

2 Obtain recent Returns (List)
https://thecloseout.labs.wesupply.xyz/api/returns/recent

3 Loop through the List

4 If return is not in the DB then Otain Return Details
https://thecloseout.labs.wesupply.xyz/api/returns/grabById?provider=Magento&reference=192424587016

5 Insert missing returns in DB (Hdr, Dtl).  Detail records needs to be one per item (e.g. quantity = 5 will generate 5 records.)






