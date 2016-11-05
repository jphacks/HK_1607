class SendExpressionDataJob < ApplicationJob
  queue_as :default

  # 先生宛てで表情の値
  def perform(data)
    LearningChannel.broadcast_to(User.find(3), {
      expression_data: data
    })
  end
end
