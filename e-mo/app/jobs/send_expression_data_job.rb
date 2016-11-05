class SendExpressionDataJob < ApplicationJob
  queue_as :default

  def perform(data)
    # 先生宛てで表情の値
    LearningChannel.broadcast_to(User.find(3), {
      expression_data: data
    })
  end
end
