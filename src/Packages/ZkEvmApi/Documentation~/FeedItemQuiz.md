# Immutable.Api.ZkEvm.Model.FeedItemQuiz

## Properties

Name | Type | Description | Notes
------------ | ------------- | ------------- | -------------
**Id** | **string** | Feed item ID | 
**Name** | **string** | Feed item name | 
**QuestId** | **string** | Quest ID | 
**Priority** | **int** | Feed item priority | 
**GemsEarnable** | **int** | Amount of gems earnable when user completes the quest | 
**Bypass** | **bool** | If the quest is bypassed, the user will not be able to see it on the feed | [optional] 
**DayZero** | **bool** | If the quest is a day0 quest | [optional] 
**GameId** | **string** | Game ID | [optional] 
**GameName** | **string** | Game name | [optional] 
**QuestCompletedPopupText** | **string** | Text to display when the quest is completed in an onboarding experience | [optional] 
**Tags** | **List&lt;string&gt;** | The tags for the feed item | [optional] 
**Categories** | **List&lt;string&gt;** | The categories for the feed item | [optional] 
**OnboardingExperience** | **string** | The onboarding experience for the feed item | [optional] 
**Type** | **string** | Feed item type | 
**QuizHeaderVideoUrl** | **string** | URL of the quiz header video | [optional] 
**QuizLogo** | **string** | URL of the quiz logo | [optional] 
**QuizQuestion** | **string** | The quiz question | 
**QuizAnswers** | **List&lt;string&gt;** | Quiz answers to display | 
**QuizCorrectAnswers** | **List&lt;int&gt;** | Quiz correct answers | 
**QuizCorrectAnswerText** | **string** | The text to display when the user answers the quiz correctly | [optional] 
**QuizHeaderInitialImage** | **string** | The initial image for the quiz header | [optional] 
**QuizHeaderAnsweredImage** | **string** | The image to display when the user answers the quiz correctly | [optional] 

[[Back to Model list]](../README.md#documentation-for-models) [[Back to API list]](../README.md#documentation-for-api-endpoints) [[Back to README]](../README.md)

