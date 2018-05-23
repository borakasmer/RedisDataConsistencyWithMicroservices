# Redis Data Consistency With Microservices

**Platform:** .Net Core

* On this applications, we will try to keep data consistency with *Redis* Pub/Sub microservices. 
* We will made .Net Core Mvc WebNews Home and Admin page. After all, when a news updated, we will publish it to an other console application. 
* It is actually a microservices. It will subscribe this updated news with `RedisNews` keyword
