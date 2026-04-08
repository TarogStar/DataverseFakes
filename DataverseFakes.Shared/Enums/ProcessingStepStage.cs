namespace FakeXrmEasy
{
    /// <summary>
    /// Defines the stages in the Dynamics 365 plugin pipeline where a plugin can be registered to execute.
    /// </summary>
    public enum ProcessingStepStage
    {
        /// <summary>
        /// Pre-validation stage (Stage 10). Executes before the main system operation and before database transactions begin.
        /// Used for early validation before any data changes occur.
        /// </summary>
        Prevalidation = 10,

        /// <summary>
        /// Pre-operation stage (Stage 20). Executes before the main system operation but within the database transaction.
        /// Used for modifying data before it is committed to the database.
        /// </summary>
        Preoperation = 20,

        /// <summary>
        /// Post-operation stage (Stage 40). Executes after the main system operation within the database transaction.
        /// Used for operations that depend on the completed database operation.
        /// </summary>
        Postoperation = 40
    }
}