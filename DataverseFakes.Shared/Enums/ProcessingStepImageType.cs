namespace FakeXrmEasy
{
    /// <summary>
    /// Defines the types of entity images that can be registered with a plugin step.
    /// Entity images provide snapshots of entity data before and/or after the core operation.
    /// </summary>
    public enum ProcessingStepImageType
    {
        /// <summary>
        /// Pre-image containing the entity attribute values before the core operation is executed.
        /// Useful for comparing old and new values or for accessing data that may be deleted.
        /// </summary>
        PreImage = 0,

        /// <summary>
        /// Post-image containing the entity attribute values after the core operation is executed.
        /// Useful for accessing the final state of the entity after modifications.
        /// </summary>
        PostImage = 1,

        /// <summary>
        /// Both pre-image and post-image are registered.
        /// Provides access to entity data both before and after the core operation.
        /// </summary>
        Both = 2
    }
}
