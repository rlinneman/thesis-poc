using FakeItEasy;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Rel.Data.Bulk;
using System.ComponentModel.DataAnnotations;
using System.Linq;

namespace Rel.Data.Test.Bulk
{
    [TestClass]
    public class ChangeValidationTests
    {
        [TestMethod]
        public void Invalid_action_fails_validation()
        {
            var ctx = A.Dummy<ValidationContext>();
            var item = new ChangeItem<object>((ChangeAction)(42), A.Dummy<object>(), A.Dummy<object>());

            var result = ChangeValidator.SanityCheck(item, ctx);

            Assert.AreNotEqual(ValidationResult.Success, result);
            Assert.IsTrue(result.MemberNames.Contains("Action"));
        }

        [TestMethod]
        public void Invalid_create_with_bfim_fails_validation()
        {
            var ctx = A.Dummy<ValidationContext>();
            var item = new ChangeItem<object>(ChangeAction.Create, A.Dummy<object>(), A.Dummy<object>());

            var result = ChangeValidator.SanityCheck(item, ctx);

            Assert.AreNotEqual(ValidationResult.Success, result);
            Assert.IsTrue(result.MemberNames.Contains("BFIM"));
        }

        [TestMethod]
        public void Invalid_create_without_afim_fails_validation()
        {
            var ctx = A.Dummy<ValidationContext>();
            var item = new ChangeItem<object>(ChangeAction.Create, null, null);

            var result = ChangeValidator.SanityCheck(item, ctx);

            Assert.AreNotEqual(ValidationResult.Success, result);
            Assert.IsTrue(result.MemberNames.Contains("AFIM"));
        }

        [TestMethod]
        public void Invalid_delete_with_afim_fails_validation()
        {
            var ctx = A.Dummy<ValidationContext>();
            var item = new ChangeItem<object>(ChangeAction.Delete, A.Dummy<object>(), A.Dummy<object>());

            var result = ChangeValidator.SanityCheck(item, ctx);

            Assert.AreNotEqual(ValidationResult.Success, result);
            Assert.IsTrue(result.MemberNames.Contains("AFIM"));
        }

        [TestMethod]
        public void Invalid_delete_without_bfim_fails_validation()
        {
            var ctx = A.Dummy<ValidationContext>();
            var item = new ChangeItem<object>(ChangeAction.Delete, null, null);

            var result = ChangeValidator.SanityCheck(item, ctx);

            Assert.AreNotEqual(ValidationResult.Success, result);
            Assert.IsTrue(result.MemberNames.Contains("BFIM"));
        }

        [TestMethod]
        public void Invalid_update_without_afim_fails_validation()
        {
            var ctx = A.Dummy<ValidationContext>();
            var item = new ChangeItem<object>(ChangeAction.Update, A.Dummy<object>(), null);

            var result = ChangeValidator.SanityCheck(item, ctx);

            Assert.AreNotEqual(ValidationResult.Success, result);
            Assert.IsTrue(result.MemberNames.Contains("AFIM"));
        }

        [TestMethod]
        public void Invalid_update_without_bfim_fails_validation()
        {
            var ctx = A.Dummy<ValidationContext>();
            var item = new ChangeItem<object>(ChangeAction.Update, null, A.Dummy<object>());

            var result = ChangeValidator.SanityCheck(item, ctx);

            Assert.AreNotEqual(ValidationResult.Success, result);
            Assert.IsTrue(result.MemberNames.Contains("BFIM"));
        }

        [TestMethod]
        public void Valid_create_passes_validation()
        {
            var ctx = A.Dummy<ValidationContext>();
            var item = new ChangeItem<object>(ChangeAction.Create, null, A.Dummy<object>());

            var result = ChangeValidator.SanityCheck(item, ctx);

            Assert.AreEqual(ValidationResult.Success, result);
        }

        [TestMethod]
        public void Valid_delete_passes_validation()
        {
            var ctx = A.Dummy<ValidationContext>();
            var item = new ChangeItem<object>(ChangeAction.Delete, A.Dummy<object>(), null);

            var result = ChangeValidator.SanityCheck(item, ctx);

            Assert.AreEqual(ValidationResult.Success, result);
        }

        [TestMethod]
        public void Valid_update_passes_validation()
        {
            var ctx = A.Dummy<ValidationContext>();
            var item = new ChangeItem<object>(ChangeAction.Update, A.Dummy<object>(), A.Dummy<object>());

            var result = ChangeValidator.SanityCheck(item, ctx);

            Assert.AreEqual(ValidationResult.Success, result);
        }
    }
}